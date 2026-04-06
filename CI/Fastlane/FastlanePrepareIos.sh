#!/bin/bash
# Script prepare jenkins for fastlane (ios)
# Environment variables from Ci system:
# WORKSPACE - absolute path to workspace

cp -rf ${WORKSPACE}/Assets/submodule-core-publishing/CI/Fastlane ${WORKSPACE}/Build/iOS/fastlane
cd ${WORKSPACE}/Build/iOS
fastlane pl_setup_jenkins_mac