#!/bin/bash
#Prepare jenkins for fastlane upload to google console
# Environment variables from Ci system:
# WORKSPACE - absolute path to workspace
# BUILD_AAB_NAME - name of artefact
# ANDROID_BUNDLE_ID - application bundle
# JSON_KEY - autorization key to upload in google console
# FIREBASE_APP_ID - id of app in firebase console

# Create android symbols zip archive 
cd ${WORKSPACE}/Build/Android/unityLibrary/symbols

BASE_DIRECTORY="${WORKSPACE}/Build/Android/unityLibrary/symbols"
ARCHIVE_NAME="${WORKSPACE}/Build/Android/unityLibrary/symbols.zip"

"C:\Program Files\7-Zip\7z.exe" a -tzip "$ARCHIVE_NAME" "$BASE_DIRECTORY\\*"

# Check if FIREBASE_APP_ID is set before uploading symbols
if [ -n "${FIREBASE_APP_ID}" ]; 
then
    echo "Uploading symbol files to Firebase Crashlytics..."    
    firebase crashlytics:symbols:upload --app="${FIREBASE_APP_ID}" "$BASE_DIRECTORY" --debug
else
    echo "FIREBASE_APP_ID is not set. Skipping Firebase Crashlytics symbols upload."
fi

# Enter in workspace folder
cd ${WORKSPACE}
# Upload artefacts in console
fastlane run supply aab:"${BUILD_AAB_NAME}" package_name:"${ANDROID_BUNDLE_ID}" json_key:"${JSON_KEY}" track:"internal" skip_upload_metadata:"true" skip_upload_images:"true" skip_upload_screenshots:"true" skip_upload_apk:"true" mapping:"${WORKSPACE}/Build/Android/unityLibrary/symbols.zip"
# Release artefacts in internal and alpha tracks - important create alpha track in google console before make release!
fastlane run supply skip_upload_aab:"true" package_name:"${ANDROID_BUNDLE_ID}" json_key:"${JSON_KEY}" track:"internal" skip_upload_metadata:"true" skip_upload_images:"true" skip_upload_screenshots:"true" skip_upload_apk:"true" track_promote_to:"alpha" deactivate_on_promote:"false"