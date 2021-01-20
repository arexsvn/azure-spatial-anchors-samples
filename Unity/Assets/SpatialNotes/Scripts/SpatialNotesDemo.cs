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
        private string currentAnchorId = "";
        private bool allowObjectPlacement = false;
        private bool readyForObjectPlacement = false;
        private bool placingNewNote = false;
        private SaveStateController saveStateController;

        public override void Start()
        {
            saveStateController = new SaveStateController();
            saveStateController.init();

            base.Start();

            if (!SanityCheckAccessConfiguration())
            {
                return;
            }

            setupUI();

            spatialNotesUI.setStatusText("Starting Session...");

            startup();
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

            if (spawnedObject != null)
            {
                spatialNotesUI.setStatusText("Tap anchor to read note.");
            }
        }

        private void handleSaveNote()
        {
            if (currentCloudAnchor == null)
            {
                SaveCurrentObjectAnchorToCloud();
                spatialNotesUI.setStatusText("Saving note...");
            }
            else
            {
                saveStateController.CurrentSave.noteText = spatialNotesUI.getText();
                saveStateController.save();
                spatialNotesUI.setStatusText("Tap anchor to read note.");
            }
            spatialNotesUI.showNoteUI(false);
        }

        private void handleDeleteAnchor()
        {
            deleteAnchor();
        }

        private async void deleteAnchor()
        {
            spatialNotesUI.setStatusText("Deleting Anchor...");
            spatialNotesUI.showNoteUI(false);
            spatialNotesUI.setNoteText(null);
            await CloudManager.DeleteAnchorAsync(currentCloudAnchor);
            CleanupSpawnedObjects();
            currentCloudAnchor = null;
            saveStateController.CurrentSave.anchorId = null;
            saveStateController.CurrentSave.noteText = null;
            saveStateController.save();
            readyForObjectPlacement = true;
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
                currentCloudAnchor = args.Anchor;

                UnityDispatcher.InvokeOnAppThread(() =>
                {
                    if (!string.IsNullOrEmpty(saveStateController.CurrentSave.noteText))
                    {
                        spatialNotesUI.setStatusText("Tap anchor to read note.");
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
                });
            }
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

                        if (!placingNewNote)
                        {
                            placingNewNote = true;
                            spatialNotesUI.setStatusText("Tap a surface to place a note.");
                        }

                    }
                    else
                    {
                        spatialNotesUI.setStatusText("Move device around room to capture more data...");
                    }
                }
            }
        }

        protected override bool IsPlacingObject()
        {
            return readyForObjectPlacement && allowObjectPlacement;
        }

  
        protected override GameObject SpawnNewAnchoredObject(Vector3 worldPos, Quaternion worldRot, CloudSpatialAnchor cloudSpatialAnchor)
        {
            readyForObjectPlacement = false;

            GameObject spawnedObject = base.SpawnNewAnchoredObject(worldPos, worldRot, cloudSpatialAnchor);

            if (placingNewNote)
            {
                placingNewNote = false;
                spatialNotesUI.setConfirmButtonText("Save Note");
                spatialNotesUI.showNoteUI(true, false, true, currentCloudAnchor != null);
                spatialNotesUI.setStatusText("Anchor placed, add a note.");
            }
            

            return spawnedObject;
        }

        private async void SaveCurrentObjectAnchorToCloud()
        {
            await SaveCurrentObjectAnchorToCloudAsync();
        }

        protected override async Task OnSaveCloudAnchorSuccessfulAsync()
        {
            await base.OnSaveCloudAnchorSuccessfulAsync();

            currentAnchorId = currentCloudAnchor.Identifier;

            spatialNotesUI.setStatusText("Note saved. Tap anchor to read it.");

            // Sanity check that the object is still where we expect
            Pose anchorPose = Pose.identity;

#if UNITY_ANDROID || UNITY_IOS
            anchorPose = currentCloudAnchor.GetPose();
#endif
            // HoloLens: The position will be set based on the unityARUserAnchor that was located.

            SpawnOrMoveCurrentAnchoredObject(anchorPose.position, anchorPose.rotation);

            saveStateController.CurrentSave.anchorId = currentAnchorId;
            saveStateController.CurrentSave.noteText = spatialNotesUI.getText();
            saveStateController.save();
        }

        protected override void OnSaveCloudAnchorFailed(Exception exception)
        {
            base.OnSaveCloudAnchorFailed(exception);

            currentAnchorId = string.Empty;
        }

        private async Task startupAsync()
        {
            if (CloudManager.Session == null)
            {
                CloudManager.SessionCreated += handleSessionCreated;
                await CloudManager.CreateSessionAsync();
            }

            currentAnchorId = "";
            currentCloudAnchor = null;
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
            Debug.Log("Session Started.");

            //if (anchorIdsToLocate.Count > 0)
            if (saveStateController.CurrentSave != null && !string.IsNullOrEmpty(saveStateController.CurrentSave.anchorId))
            {
                Debug.Log("Creating watcher to find anchor id : " + saveStateController.CurrentSave.anchorId);
                /*
                const float distanceInMeters = .5f;
                const int maxAnchorsToFind = 25;
                SetNearDevice(distanceInMeters, maxAnchorsToFind);
                anchorLocateCriteria.Strategy = LocateStrategy.VisualInformation;
                */
                //anchorLocateCriteria.Strategy = LocateStrategy.AnyStrategy;

                spatialNotesUI.setStatusText("Locating saved notes...");

                List<string> anchorsToFind = new List<string> { saveStateController.CurrentSave.anchorId };

                SetAnchorIdsToLocate(anchorsToFind);

                currentWatcher = CreateWatcher();
            }
            else
            {
                readyForObjectPlacement = true;
            }
        }
  
        protected override void OnAnchorInteraction(CloudNativeAnchor anchor)
        {
            if (saveStateController.CurrentSave != null && !string.IsNullOrEmpty(saveStateController.CurrentSave.anchorId) && anchor.CloudAnchor.Identifier == saveStateController.CurrentSave.anchorId)
            {
                spatialNotesUI.setNoteText(saveStateController.CurrentSave.noteText);
                spawnedObjectMat.color = Color.yellow;
                spatialNotesUI.setConfirmButtonText("Update Note");
                spatialNotesUI.showNoteUI(true, true, true, currentCloudAnchor != null);
            }
        }

        protected override void SpawnOrMoveCurrentAnchoredObject(Vector3 worldPos, Quaternion worldRot)
        {
            bool spawnedNewObject = spawnedObject == null;

            base.SpawnOrMoveCurrentAnchoredObject(worldPos, worldRot);

            readyForObjectPlacement = false;

            /*
            if (spawnedNewObject)
            {
                notesEditorView.show(true);
            }
            */

            /*
            if (currentCloudAnchor != null && spawnedObjectsInCurrentAppState.ContainsKey(currentCloudAnchor.Identifier))
            {
                spawnedObject = spawnedObjectsInCurrentAppState[currentCloudAnchor.Identifier];
            }

            bool spawnedNewObject = spawnedObject == null;

            base.SpawnOrMoveCurrentAnchoredObject(worldPos, worldRot);

            if (spawnedNewObject)
            {
                allSpawnedObjects.Add(spawnedObject);
                allSpawnedMaterials.Add(spawnedObjectMat);

                if (currentCloudAnchor != null && spawnedObjectsInCurrentAppState.ContainsKey(currentCloudAnchor.Identifier) == false)
                {
                    spawnedObjectsInCurrentAppState.Add(currentCloudAnchor.Identifier, spawnedObject);
                }
            }

#if WINDOWS_UWP || UNITY_WSA
            if (currentCloudAnchor != null
                    && spawnedObjectsInCurrentAppState.ContainsKey(currentCloudAnchor.Identifier) == false)
            {
                spawnedObjectsInCurrentAppState.Add(currentCloudAnchor.Identifier, spawnedObject);
            }
#endif
            */
        }

        protected override Color GetStepColor()
        {
            return Color.clear;
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
