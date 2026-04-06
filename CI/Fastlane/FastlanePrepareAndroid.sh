#!/bin/bash
# Script prepare jenkins for fastlane upload to google console
# Environment variables from Ci system:
# BUILD_AAB_NAME - upload file name
# ANDROID_BUNDLE_ID - application bundle id
# JSON_KEY - application json key


cd ${WORKSPACE}
fastlane run supply aab:"${BUILD_AAB_NAME}" package_name:"${ANDROID_BUNDLE_ID}" json_key:"${JSON_KEY}" track:"internal" skip_upload_metadata:"true" skip_upload_images:"true" skip_upload_screenshots:"true" skip_upload_apk:"true"
fastlane run supply skip_upload_aab:"true" package_name:"${ANDROID_BUNDLE_ID}" json_key:"${JSON_KEY}" track:"internal" skip_upload_metadata:"true" skip_upload_images:"true" skip_upload_screenshots:"true" skip_upload_apk:"true" track_promote_to:"alpha" deactivate_on_promote:"false"
