#!/bin/bash
# Script for uploading ipa or apk file to https://firebase.google.com/docs/app-distribution
# Environment variables from Ci system:
# GIT_PREVIOUS_SUCCESSFUL_COMMIT - id of last successfull build's Commit
# GIT_COMMIT - id of current build commit
# FIREBASE_APP_ID - id of app in firebase console
# BUILD_FILE_NAME - name with full path of apk or ipa build file
# BUILD_FILE_APK_PATH - path to find apk of build
# BUILD_FILE_IPA_PATH - path to find ipa of build

buildname=${BUILD_FILE_NAME}
if [[ "$buildname" == "" ]]; 
then
	if [[ "${BUILD_FILE_APK_PATH}" != "" ]]; 
    then
            buildname=$(find ${BUILD_FILE_APK_PATH} -type f -name '*.apk')
    fi
    if [[ "$buildname" == "" ]];
    then
        if [[ "${BUILD_FILE_IPA_PATH}" != "" ]];
        then
            buildname=$(find ${BUILD_FILE_IPA_PATH} -type f -name '*.ipa')
        fi
        if [[ "$buildname" == "" ]];
        then
            if [[ "${BUILD_FILE_APK2019_PATH}" != "" ]];
            then
                buildname=$(find ${BUILD_FILE_APK2019_PATH} -type f -name '*.apk')
            fi
        fi
    fi
fi
echo 'buildname= '$buildname

#basic functions
function fail {
        echo "$*" >&2
        exit 1
}

function section_print {
	echo -e "\n=== $* ==="
}

#some magic with json parsing
function jsonval {
   echo $1 | python -c 'import sys, json; print (json.load(sys.stdin)["'$2'"])';
}


#configuration
changeslog=$(git log ${GIT_PREVIOUS_SUCCESSFUL_COMMIT}..${GIT_COMMIT} --pretty="- %s " --no-merges)
num=${#changeslog}
maxval=5000
if [[ $num -gt $maxval ]];
then
	changeslog=${changeslog:0:4990}
	num= ${#changeslog}
	echo 'crop change log to '$num
fi

#main
cd ${WORKSPACE}

firebase appdistribution:distribute $buildname --app ${FIREBASE_APP_ID} --release-notes "'$changeslog'" --testers-file "Assets/submodule-continuous-integration/FirebaseAppDistribution/testers.txt"

if [[ "${FIREBASE_APP_ID}" =~ :android: ]];
then
  echo "Uploading symbol files to Firebase Crashlytics..."
  BASE_DIRECTORY="${WORKSPACE}/Build/Android/unityLibrary/symbols"
  firebase crashlytics:symbols:upload --app="${FIREBASE_APP_ID}" "$BASE_DIRECTORY" --debug
fi