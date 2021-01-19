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
        internal enum AppState
        {
            DemoStepCreateSession = 0,
            DemoStepConfigSession,
            DemoStepStartSession,
            DemoStepCreateLocalAnchor,
            DemoStepSaveCloudAnchor,
            DemoStepSavingCloudAnchor,
            DemoStepStopSession,
            DemoStepDestroySession,
            DemoStepCreateSessionForQuery,
            DemoStepStartSessionForQuery,
            DemoStepLookForAnchor,
            DemoStepLookingForAnchor,
            DemoStepDeleteFoundAnchor,
            DemoStepStopSessionForQuery,
            DemoStepComplete
        }

        private readonly Dictionary<AppState, DemoStepParams> stateParams = new Dictionary<AppState, DemoStepParams>
        {
            { AppState.DemoStepCreateSession,new DemoStepParams() { StepMessage = "Next: Create Azure Spatial Anchors Session", StepColor = Color.clear }},
            { AppState.DemoStepConfigSession,new DemoStepParams() { StepMessage = "Next: Configure Azure Spatial Anchors Session", StepColor = Color.clear }},
            { AppState.DemoStepStartSession,new DemoStepParams() { StepMessage = "Next: Start Azure Spatial Anchors Session", StepColor = Color.clear }},
            { AppState.DemoStepCreateLocalAnchor,new DemoStepParams() { StepMessage = "Tap a surface to add the Local Anchor.", StepColor = Color.blue }},
            { AppState.DemoStepSaveCloudAnchor,new DemoStepParams() { StepMessage = "Next: Save Local Anchor to cloud", StepColor = Color.yellow }},
            { AppState.DemoStepSavingCloudAnchor,new DemoStepParams() { StepMessage = "Saving local Anchor to cloud...", StepColor = Color.yellow }},
            { AppState.DemoStepStopSession,new DemoStepParams() { StepMessage = "Next: Stop Azure Spatial Anchors Session", StepColor = Color.green }},
            { AppState.DemoStepCreateSessionForQuery,new DemoStepParams() { StepMessage = "Next: Create Azure Spatial Anchors Session for query", StepColor = Color.clear }},
            { AppState.DemoStepStartSessionForQuery,new DemoStepParams() { StepMessage = "Next: Start Azure Spatial Anchors Session for query", StepColor = Color.clear }},
            { AppState.DemoStepLookForAnchor,new DemoStepParams() { StepMessage = "Next: Look for Anchor", StepColor = Color.clear }},
            { AppState.DemoStepLookingForAnchor,new DemoStepParams() { StepMessage = "Looking for Anchor...", StepColor = Color.clear }},
            { AppState.DemoStepDeleteFoundAnchor,new DemoStepParams() { StepMessage = "Next: Delete Anchor", StepColor = Color.yellow }},
            { AppState.DemoStepStopSessionForQuery,new DemoStepParams() { StepMessage = "Next: Stop Azure Spatial Anchors Session for query", StepColor = Color.grey }},
            { AppState.DemoStepComplete,new DemoStepParams() { StepMessage = "Next: Restart demo", StepColor = Color.clear }}
        };

        private AppState _currentAppState = AppState.DemoStepCreateSession;

        AppState currentAppState
        {
            get
            {
                return _currentAppState;
            }
            set
            {
                if (_currentAppState != value)
                {
                    Debug.LogFormat("State from {0} to {1}", _currentAppState, value);
                    _currentAppState = value;
                    if (spawnedObjectMat != null)
                    {
                        spawnedObjectMat.color = stateParams[_currentAppState].StepColor;
                    }

                    if (!isErrorActive)
                    {
                        feedbackBox.text = stateParams[_currentAppState].StepMessage;
                    }
                }
            }
        }

        [SerializeField] private NotesEditorView notesEditorView;
        private string currentAnchorId = "";
        private bool allowObjectPlacement = false;
        private SaveStateController saveStateController;

        /// <summary>
        /// Start is called on the frame when a script is enabled just before any
        /// of the Update methods are called the first time.
        /// </summary>
        public override void Start()
        {
            Debug.Log(">>Azure Spatial Anchors Demo Script Start");

            saveStateController = new SaveStateController();
            saveStateController.init();

            base.Start();

            if (!SanityCheckAccessConfiguration())
            {
                return;
            }
            feedbackBox.text = stateParams[currentAppState].StepMessage;

            Debug.Log("Azure Spatial Anchors Demo script started");

            setupUI();

            startup();
        }

        private void setupUI()
        {
            notesEditorView.saveButton.onClick.AddListener(handleSaveNote);
            notesEditorView.backButton.onClick.AddListener(handleBack);
        }

        private void handleBack()
        {
            notesEditorView.show(false);

            if (string.IsNullOrEmpty(saveStateController.CurrentSave.anchorId))
            {
                allowObjectPlacement = true;
            }
        }

        private void handleSaveNote()
        {
            SaveCurrentObjectAnchorToCloud();
            notesEditorView.show(false);
            //allowObjectPlacement = true;
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
                    currentAppState = AppState.DemoStepDeleteFoundAnchor;
                    Pose anchorPose = Pose.identity;

#if UNITY_ANDROID || UNITY_IOS
                    anchorPose = currentCloudAnchor.GetPose();
#endif
                    // HoloLens: The position will be set based on the unityARUserAnchor that was located.
                    SpawnOrMoveCurrentAnchoredObject(anchorPose.position, anchorPose.rotation);
                });
            }
        }

        /// <summary>
        /// Update is called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        public override void Update()
        {
            base.Update();

            if (spawnedObjectMat != null)
            {
                float rat = 0.1f;
                float createProgress = 0f;
                if (CloudManager.SessionStatus != null)
                {
                    createProgress = CloudManager.SessionStatus.RecommendedForCreateProgress;
                }
                rat += (Mathf.Min(createProgress, 1) * 0.9f);
                spawnedObjectMat.color = GetStepColor() * rat;
            }
        }

        protected override bool IsPlacingObject()
        {
            return allowObjectPlacement;
            //return currentAppState == AppState.DemoStepCreateLocalAnchor;
        }

  
        protected override GameObject SpawnNewAnchoredObject(Vector3 worldPos, Quaternion worldRot, CloudSpatialAnchor cloudSpatialAnchor)
        {
            allowObjectPlacement = false;

            GameObject spawnedObject = base.SpawnNewAnchoredObject(worldPos, worldRot, cloudSpatialAnchor);

            if (cloudSpatialAnchor == null)
            {
                notesEditorView.show(true);
            }
            

            return spawnedObject;
        }

        private async void SaveCurrentObjectAnchorToCloud()
        {
            await SaveCurrentObjectAnchorToCloudAsync();
        }

        protected override Color GetStepColor()
        {
            return stateParams[currentAppState].StepColor;
        }

        protected override async Task OnSaveCloudAnchorSuccessfulAsync()
        {
            await base.OnSaveCloudAnchorSuccessfulAsync();

            currentAnchorId = currentCloudAnchor.Identifier;

            // Sanity check that the object is still where we expect
            Pose anchorPose = Pose.identity;

#if UNITY_ANDROID || UNITY_IOS
            anchorPose = currentCloudAnchor.GetPose();
#endif
            // HoloLens: The position will be set based on the unityARUserAnchor that was located.

            SpawnOrMoveCurrentAnchoredObject(anchorPose.position, anchorPose.rotation);

            saveStateController.CurrentSave.anchorId = currentAnchorId;
            saveStateController.CurrentSave.noteText = notesEditorView.getText();
            saveStateController.save();

            currentAppState = AppState.DemoStepStopSession;
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
                Debug.Log("Creating Session...");
                CloudManager.SessionCreated += handleSessionCreated;
                await CloudManager.CreateSessionAsync();
            }

            currentAnchorId = "";
            currentCloudAnchor = null;

            //Debug.Log("Configure Session...");
            //ConfigureSession();

            //await CloudManager.StartSessionAsync();
        }

        private async void handleSessionCreated(object sender, EventArgs args)
        {
            Debug.Log("Starting Session...");

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


                List<string> anchorsToFind = new List<string>();
                if (saveStateController.CurrentSave != null && !string.IsNullOrEmpty(saveStateController.CurrentSave.anchorId))
                {
                    Debug.Log("ConfigureSession :: found anchor id : " + saveStateController.CurrentSave.anchorId);
                    anchorsToFind.Add(saveStateController.CurrentSave.anchorId);
                }
                else
                {
                    Debug.Log("ConfigureSession :: no saved anchors found.");
                }

                SetAnchorIdsToLocate(anchorsToFind);

                currentWatcher = CreateWatcher();
            }
            else
            {
                allowObjectPlacement = true;
            }
        }

        public async override Task AdvanceDemoAsync()
        {
            /*
            switch (currentAppState)
            {
                case AppState.DemoStepCreateSession:
                    if (CloudManager.Session == null)
                    {
                        await CloudManager.CreateSessionAsync();
                    }
                    currentAnchorId = "";
                    currentCloudAnchor = null;
                    currentAppState = AppState.DemoStepConfigSession;
                    break;
                case AppState.DemoStepConfigSession:
                    ConfigureSession();
                    currentAppState = AppState.DemoStepStartSession;
                    break;
                case AppState.DemoStepStartSession:
                    await CloudManager.StartSessionAsync();
                    currentAppState = AppState.DemoStepCreateLocalAnchor;
                    break;
                case AppState.DemoStepCreateLocalAnchor:
                    if (spawnedObject != null)
                    {
                        currentAppState = AppState.DemoStepSaveCloudAnchor;
                    }
                    break;
                case AppState.DemoStepSaveCloudAnchor:
                    currentAppState = AppState.DemoStepSavingCloudAnchor;
                    await SaveCurrentObjectAnchorToCloudAsync();
                    break;
                case AppState.DemoStepStopSession:
                    CloudManager.StopSession();
                    CleanupSpawnedObjects();
                    await CloudManager.ResetSessionAsync();
                    currentAppState = AppState.DemoStepCreateSessionForQuery;
                    break;
                case AppState.DemoStepCreateSessionForQuery:
                    ConfigureSession();
                    currentAppState = AppState.DemoStepStartSessionForQuery;
                    break;
                case AppState.DemoStepStartSessionForQuery:
                    await CloudManager.StartSessionAsync();
                    currentAppState = AppState.DemoStepLookForAnchor;
                    break;
                case AppState.DemoStepLookForAnchor:
                    currentAppState = AppState.DemoStepLookingForAnchor;
                    currentWatcher = CreateWatcher();
                    break;
                case AppState.DemoStepLookingForAnchor:
                    break;
                case AppState.DemoStepDeleteFoundAnchor:
                    await CloudManager.DeleteAnchorAsync(currentCloudAnchor);
                    currentAppState = AppState.DemoStepStopSessionForQuery;
                    CleanupSpawnedObjects();
                    break;
                case AppState.DemoStepStopSessionForQuery:
                    CloudManager.StopSession();
                    currentWatcher = null;
                    currentAppState = AppState.DemoStepComplete;
                    break;
                case AppState.DemoStepComplete:
                    currentCloudAnchor = null;
                    currentAppState = AppState.DemoStepCreateSession;
                    CleanupSpawnedObjects();
                    break;
                default:
                    Debug.Log("Shouldn't get here for app state " + currentAppState.ToString());
                    break;
            }
            */
            }

        /*
        private void ConfigureSession()
        {
            List<string> anchorsToFind = new List<string>();
            //if (currentAppState == AppState.DemoStepCreateSessionForQuery)
            if (saveStateController.CurrentSave != null && !string.IsNullOrEmpty(saveStateController.CurrentSave.anchorId))
            {
                Debug.Log("ConfigureSession :: found anchor id : " + saveStateController.CurrentSave.anchorId);
                anchorsToFind.Add(saveStateController.CurrentSave.anchorId);
            }
            else
            {
                Debug.Log("ConfigureSession :: no saved anchors found.");
            }

            SetAnchorIdsToLocate(anchorsToFind);
        }
        */

        protected override void OnAnchorInteraction(CloudNativeAnchor anchor)
        {
            if (saveStateController.CurrentSave != null && !string.IsNullOrEmpty(saveStateController.CurrentSave.anchorId) && anchor.CloudAnchor.Identifier == saveStateController.CurrentSave.anchorId)
            {
                notesEditorView.setFoundNote(saveStateController.CurrentSave.noteText);
            }
        }

        protected override void SpawnOrMoveCurrentAnchoredObject(Vector3 worldPos, Quaternion worldRot)
        {
            bool spawnedNewObject = spawnedObject == null;

            base.SpawnOrMoveCurrentAnchoredObject(worldPos, worldRot);

            allowObjectPlacement = false;

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
