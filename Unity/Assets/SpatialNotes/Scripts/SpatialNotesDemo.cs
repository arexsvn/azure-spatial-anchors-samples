// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.Azure.SpatialAnchors.Unity.Examples
{
    public class SpatialNotesDemo : DemoScriptBase
    {
        private string currentAnchorId = null;
        private bool allowObjectPlacement = false;
        private bool readyForObjectPlacement = false;
        private SaveStateController saveStateController;
        private Dictionary<string, GameObject> savedCloudAnchorsById = new Dictionary<string, GameObject>();

        public override void Start()
        {
            saveStateController = new SaveStateController();
            saveStateController.init();

            // Don't let the screen shutoff while running app.
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            base.Start();

            if (!SanityCheckAccessConfiguration())
            {
                return;
            }
            
            setupUI();

            spatialNotesUI.setStatusText("Starting Session...");

            startup();
        }

        public override void Update()
        {
            base.Update();

            if (CloudManager.SessionStatus != null)
            {
                if (spatialNotesUI != null)
                {
                    spatialNotesUI.setConnection(CloudManager.SessionStatus.RecommendedForCreateProgress);
                }

                if (readyForObjectPlacement)
                {
                    if (CloudManager.SessionStatus.RecommendedForCreateProgress >= 1f)
                    {
                        allowObjectPlacement = true;
                        showPlacementInfo();

                    }
                    else
                    {
                        spatialNotesUI.setStatusText("Move device around room to capture more data...");
                    }
                }
            }
        }

        private void setupUI()
        {
            spatialNotesUI.saveButton.onClick.AddListener(handleSaveNote);
            spatialNotesUI.backButton.onClick.AddListener(handleBack);
            spatialNotesUI.deleteButton.onClick.AddListener(handleDeleteAnchor);
            spatialNotesUI.showNoteUI(false);
        }

        private void handleBack()
        {
            spatialNotesUI.showNoteUI(false);
            setAnchorSelected(null);
            currentAnchorId = null;
            enableAnchorPlacement();
        }

        private void handleSaveNote()
        {
            if (currentAnchorId != null)
            {
                saveStateController.addNote(currentAnchorId, spatialNotesUI.getText());
                saveStateController.save();
                handleBack();
            }
            else
            {
                spatialNotesUI.setStatusText("Saving new note...");
                SaveCurrentObjectAnchorToCloud();
                spatialNotesUI.showNoteUI(false);
            }
        }

        private void handleDeleteAnchor()
        {
            deleteAnchor();
        }

        private void setAnchorSelected(string selectedAnchor)
        {
            foreach(KeyValuePair<string, GameObject> kvp in savedCloudAnchorsById)
            {                
                if (kvp.Value == null)
                {
                    Debug.LogError("Anchor GameObject is null for id " + kvp.Key + " selectedAnchor : "+ selectedAnchor);
                    continue;
                }

                if (selectedAnchor != null && kvp.Key == selectedAnchor)
                {
                    kvp.Value.GetComponent<MeshRenderer>().material.color = Color.yellow;
                }
                else
                {
                    kvp.Value.GetComponent<MeshRenderer>().material.color = Color.blue;
                }
            }
        }

        protected override void AnchorNotLocated(string identifier)
        {
            Debug.LogError("AnchorNotLocated " + identifier + ". Deleting.");

            deleteAnchor(identifier);
        }

        protected override void OnSelectObjectInteraction(Vector3 hitPoint, object target, GameObject gameObject = null)
        {
            // block all interactions when the note ui is up
            if (!spatialNotesUI.showingNoteUI)
            {
                base.OnSelectObjectInteraction(hitPoint, target, gameObject);
            }
        }

        private async void deleteAnchor(string identifier = null)
        {
            if (identifier == null)
            {
                identifier = currentAnchorId;
            }
            
            // Remove saved notes associated with an anchor in addition to the local and cloud instances of the anchor.
            spatialNotesUI.setStatusText("Deleting Anchor...");
            spatialNotesUI.showNoteUI(false);
            spatialNotesUI.setNoteText(null);
            saveStateController.removeNote(identifier);

            if (savedCloudAnchorsById.ContainsKey(identifier))
            {
                await CloudManager.DeleteAnchorAsync(savedCloudAnchorsById[identifier].GetComponent<CloudNativeAnchor>().CloudAnchor);
                Destroy(savedCloudAnchorsById[identifier]);
                savedCloudAnchorsById.Remove(identifier);
            }

            currentAnchorId = null;

            enableAnchorPlacement();
        }

        private async void enableAnchorPlacement()
        {
            // Add a delay to prevent accidental clicks and give time for status messages to be read.
            await Task.Delay(500);

            showPlacementInfo();

            readyForObjectPlacement = true;
        }

        private void showPlacementInfo()
        {
            if (saveStateController.getSavedAnchorIds().Count > 0)
            {
                spatialNotesUI.setStatusText("Tap anchor to read note or place a new one.");
            }
            else
            {
                spatialNotesUI.setStatusText("Tap a surface to leave a new note.");
            }
        }

        private async void startup()
        {
            await startupAsync();
        }

        protected override void OnCloudAnchorLocated(AnchorLocatedEventArgs args)
        {
            base.OnCloudAnchorLocated(args);

            if (args.Status == LocateAnchorStatus.Located)
            {
                UnityDispatcher.InvokeOnAppThread(() =>
                {
                    currentCloudAnchor = args.Anchor;

                    if (!string.IsNullOrEmpty(saveStateController.getNoteText(currentCloudAnchor.Identifier)))
                    {
                        spatialNotesUI.setStatusText("Tap anchor to read note or place a new one.");
                    }
                    else
                    {
                        spatialNotesUI.setStatusText("Found anchor.");
                    }

                    Pose anchorPose = Pose.identity;

#if UNITY_ANDROID || UNITY_IOS
                    anchorPose = currentCloudAnchor.GetPose();
#endif
                    // HoloLens: The position will be set based on the unityARUserAnchor that was located.
                    SpawnOrMoveCurrentAnchoredObject(anchorPose.position, anchorPose.rotation);

                    if (!savedCloudAnchorsById.ContainsKey(currentCloudAnchor.Identifier))
                    {
                        savedCloudAnchorsById[currentCloudAnchor.Identifier] = spawnedObject;
                    }

                    spawnedObject = null;
                    currentCloudAnchor = null;
                    spawnedObjectMat = null;
                });
            }
        }

        protected override bool IsPlacingObject()
        {
            return readyForObjectPlacement && allowObjectPlacement && !spatialNotesUI.showingNoteUI;
        }

        protected override void NewAnchorPlaced()
        {
            readyForObjectPlacement = false;

            if (currentCloudAnchor == null)
            {
                spatialNotesUI.setNoteText(null);
                spatialNotesUI.setConfirmButtonText("Save Note");
                spatialNotesUI.showNoteUI(true, false, true, false);
                spatialNotesUI.setStatusText("Anchor placed, add a note.");
            }
        }

        private async void SaveCurrentObjectAnchorToCloud()
        {
            await SaveCurrentObjectAnchorToCloudAsync();
        }

        protected override async Task OnSaveCloudAnchorSuccessfulAsync()
        {
            await base.OnSaveCloudAnchorSuccessfulAsync();

            spatialNotesUI.setStatusText("Note saved!");

            // Sanity check that the object is still where we expect
            Pose anchorPose = Pose.identity;

#if UNITY_ANDROID || UNITY_IOS
            anchorPose = currentCloudAnchor.GetPose();
#endif
            // HoloLens: The position will be set based on the unityARUserAnchor that was located.

            SpawnOrMoveCurrentAnchoredObject(anchorPose.position, anchorPose.rotation);

            saveStateController.addNote(currentCloudAnchor.Identifier, spatialNotesUI.getText());

            // Cleanup global references and store spawned object for later interactions.
            savedCloudAnchorsById[currentCloudAnchor.Identifier] = spawnedObject;

            spawnedObject = null;
            currentCloudAnchor = null;
            spawnedObjectMat = null;

            enableAnchorPlacement();
        }

        private async Task startupAsync()
        {
            if (CloudManager.Session == null)
            {
                CloudManager.SessionCreated += handleSessionCreated;
                await CloudManager.CreateSessionAsync();
            }
        }

        private async void handleSessionCreated(object sender, EventArgs args)
        {
            await CloudManager.StartSessionAsync();

            CloudManager.SessionCreated -= handleSessionCreated;
            /*
            locationProvider = new PlatformLocationProvider();
            CloudManager.Session.LocationProvider = locationProvider;
            SensorPermissionHelper.RequestSensorPermissions();
            ConfigureSensors();
            */

            if (saveStateController.CurrentSave != null && saveStateController.getSavedAnchorIds().Count > 0)
            {
                /*
                Debug.Log("Creating watcher to find anchor id : " + saveStateController.CurrentSave.anchorId);
                
                const float distanceInMeters = .5f;
                const int maxAnchorsToFind = 25;
                SetNearDevice(distanceInMeters, maxAnchorsToFind);
                anchorLocateCriteria.Strategy = LocateStrategy.VisualInformation;
                */
                //anchorLocateCriteria.Strategy = LocateStrategy.AnyStrategy;

                spatialNotesUI.setStatusText("Locating saved notes...");

                SetAnchorIdsToLocate(saveStateController.getSavedAnchorIds());

                currentWatcher = CreateWatcher();
            }
            else
            {
                enableAnchorPlacement();
            }
        }
  
        protected override void OnAnchorInteraction(CloudNativeAnchor anchor)
        {
            if (anchor.CloudAnchor != null)
            {
                readyForObjectPlacement = false;
                currentAnchorId = anchor.CloudAnchor.Identifier;

                spatialNotesUI.setNoteText(saveStateController.getNoteText(currentAnchorId));
                setAnchorSelected(currentAnchorId);
                spatialNotesUI.setConfirmButtonText("Update Note");
                spatialNotesUI.setStatusText("Update the text of a saved note.");
                spatialNotesUI.showNoteUI(true, true, true, true);
            }
            else
            {
                Debug.LogError("OnAnchorInteraction :: anchor has not been saved to the cloud.");
            }
        }

        protected override Color GetStepColor()
        {
            return Color.blue;
        }

        public async override Task AdvanceDemoAsync()
        {
            // not needed...
        }

        /*
        private PlatformLocationProvider locationProvider;

        public void OnApplicationFocus(bool focusStatus)
        {
#if UNITY_ANDROID
            // We may get additional permissions at runtime. Enable the sensors once app is resumed
            if (focusStatus && locationProvider != null)
            {
                ConfigureSensors();
            }
#endif
        }

        private void ConfigureSensors()
        {
            locationProvider.Sensors.GeoLocationEnabled = SensorPermissionHelper.HasGeoLocationPermission();

            locationProvider.Sensors.WifiEnabled = SensorPermissionHelper.HasWifiPermission();

            locationProvider.Sensors.BluetoothEnabled = SensorPermissionHelper.HasBluetoothPermission();
            locationProvider.Sensors.KnownBeaconProximityUuids = CoarseRelocSettings.KnownBluetoothProximityUuids;
        }

        public SensorStatus GeoLocationStatus
        {
            get
            {
                if (locationProvider == null)
                    return SensorStatus.MissingSensorFingerprintProvider;
                if (!locationProvider.Sensors.GeoLocationEnabled)
                    return SensorStatus.DisabledCapability;
                switch (locationProvider.GeoLocationStatus)
                {
                    case GeoLocationStatusResult.Available:
                        return SensorStatus.Available;
                    case GeoLocationStatusResult.DisabledCapability:
                        return SensorStatus.DisabledCapability;
                    case GeoLocationStatusResult.MissingSensorFingerprintProvider:
                        return SensorStatus.MissingSensorFingerprintProvider;
                    case GeoLocationStatusResult.NoGPSData:
                        return SensorStatus.NoData;
                    default:
                        return SensorStatus.MissingSensorFingerprintProvider;
                }
            }
        }

        public SensorStatus WifiStatus
        {
            get
            {
                if (locationProvider == null)
                    return SensorStatus.MissingSensorFingerprintProvider;
                if (!locationProvider.Sensors.WifiEnabled)
                    return SensorStatus.DisabledCapability;
                switch (locationProvider.WifiStatus)
                {
                    case WifiStatusResult.Available:
                        return SensorStatus.Available;
                    case WifiStatusResult.DisabledCapability:
                        return SensorStatus.DisabledCapability;
                    case WifiStatusResult.MissingSensorFingerprintProvider:
                        return SensorStatus.MissingSensorFingerprintProvider;
                    case WifiStatusResult.NoAccessPointsFound:
                        return SensorStatus.NoData;
                    default:
                        return SensorStatus.MissingSensorFingerprintProvider;
                }
            }
        }

        public SensorStatus BluetoothStatus
        {
            get
            {
                if (locationProvider == null)
                    return SensorStatus.MissingSensorFingerprintProvider;
                if (!locationProvider.Sensors.BluetoothEnabled)
                    return SensorStatus.DisabledCapability;
                switch (locationProvider.BluetoothStatus)
                {
                    case BluetoothStatusResult.Available:
                        return SensorStatus.Available;
                    case BluetoothStatusResult.DisabledCapability:
                        return SensorStatus.DisabledCapability;
                    case BluetoothStatusResult.MissingSensorFingerprintProvider:
                        return SensorStatus.MissingSensorFingerprintProvider;
                    case BluetoothStatusResult.NoBeaconsFound:
                        return SensorStatus.NoData;
                    default:
                        return SensorStatus.MissingSensorFingerprintProvider;
                }
            }
        }
        */
    }
}
