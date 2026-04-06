#!/bin/bash
# Script upload MacOS package to Mac Appstore
# Environment variables from Ci system:
# WORKSPACE - absolute path to workspace

yes | cp -rf ${WORKSPACE}/Assets/submodule-continuous-integration/fastlane ${WORKSPACE}/Build || true
cd ${WORKSPACE}/Build

fastlane mac pl_setup_jenkins_mac

fastlane mac pl_deliver_pkg_to_macstore