#!/bin/bash
# Script prepare jenkins for fastlane (MacOS)
# Environment variables from Ci system:
# WORKSPACE - absolute path to workspace
# PROJECT_NAME - name of app file

yes | mv "${WORKSPACE}/Build/OSXApp.app" "${WORKSPACE}/Build/${PROJECT_NAME}.app"
yes | mv "${WORKSPACE}/Build/OSXApp_BackUpThisFolder_ButDontShipItWithYourGame" "${WORKSPACE}/Build/${PROJECT_NAME}_BackUpThisFolder_ButDontShipItWithYourGame" || true

yes | cp -rf ${WORKSPACE}/Assets/submodule-continuous-integration/fastlane ${WORKSPACE}/Build
cd ${WORKSPACE}/Build

fastlane pl_setup_jenkins_mac

