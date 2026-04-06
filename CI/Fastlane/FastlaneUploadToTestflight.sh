#!/bin/bash
# Script prepare jenkins for fastlane (ios)
# Environment variables from Ci system:
# WORKSPACE - absolute path to workspace
# BUILD_NUMBER - build number

cd ${WORKSPACE}/Build/iOS
fastlane pl_build_and_upload_testflight