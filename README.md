## Spatial Notes AR Demo

This demo will allow a user to place anchors in the environment and associate a note with them. Notes can be edited or deleted.
Code, assets and main 'SpatialNotes' scene for the project are in Unity/Assets/SpatialNotes. 

### New User Flow
1. Start AR Session.
2. Wait for environment data scan to reach recommended completion for saving an anchor.
3. Place an anchor in the room.
4. Add a note to the anchor.
5. Save the note and anchor id to device.
6. Tap on the anchor to delete it or edit the note. A new anchor can be placed after deleting the existing one.
7. Any number of notes can be added by adding new anchors.

### Returning User Flow
1. Start AR Session.
2. Load saved data (anchor id and note text.)
3. Display saved anchors.
4. Tap on the anchor to delete it or edit the note. A new anchor can be placed after deleting the existing one.
