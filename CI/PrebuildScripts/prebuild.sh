#!/bin/bash
#this file for common SH/BASH scripts that need to be executed before any other build actions (prebuild.sh)

echo "Start prebuild script"

########## CHARTBOOST CLEANUP BOF ##########
# Removes the Chartboost mediation plugin from MaxSdk if the CI job
# defines the environment variable REMOVE_CHARTBOOST=true.
# WORKSPACE – absolute path to the workspace, supplied by the CI system.

chartboostPath="${WORKSPACE}/Assets/submodule-core-publishing/3rdPartyTools/MaxSdk/Mediation/Chartboost"

if [[ "${REMOVE_CHARTBOOST}" == "true" ]]; then
    echo "REMOVE_CHARTBOOST=true → deleting Chartboost from project."
    if [[ -d "${chartboostPath}" ]]; then
        rm -rf "${chartboostPath}"
        echo "Chartboost plugin successfully removed: ${chartboostPath}"
    else
        echo "Chartboost plugin not found at: ${chartboostPath}"
    fi
else
    echo "REMOVE_CHARTBOOST is not true → skipping Chartboost deletion."
fi
########## CHARTBOOST CLEANUP EOF ##########
########## FIREBASE BOF ##########
# Script for moving Firebase files from submodule-core to Assets
# Environment variables from Ci system:
# WORKSPACE - absolute path to workspace

generalLocalRepoPath=${WORKSPACE}/Assets/GeneralLocalRepo
firebaseAssetsPath=${WORKSPACE}/Assets/Firebase
firebasePluginsPath=${WORKSPACE}/Assets/Plugins/Android/FirebaseApp.androidlib
firebaseCorePath=${WORKSPACE}/Assets/submodule-core-publishing/3rdPartyTools/Firebase

firebasePluginsForMovePath=${WORKSPACE}/Assets/submodule-core-publishing/3rdPartyTools/Firebase/PluginsForMoving/Android
firebasePluginsTargetPath=${WORKSPACE}/Assets/Plugins

applovinAssetsPath=${WORKSPACE}/Assets/MaxSdk
applovinPluginsPath=${WORKSPACE}/Assets/Plugins/Android
applovinCorePath=${WORKSPACE}/Assets/submodule-core-publishing/3rdPartyTools/MaxSdk/AppLovin/Plugins/Android/MaxMediationGoogle.androidlib
applovinCorePath2=${WORKSPACE}/Assets/submodule-core-publishing/3rdPartyTools/MaxSdk/AppLovin/Plugins/Android/MaxMediationGoogleAdManager.androidlib

amazonAssetsPath=${WORKSPACE}/Assets/Amazon
amazonCorePath=${WORKSPACE}/Assets/submodule-core-publishing/3rdPartyTools/Amazon

echo "Start prebuild firebase section"
echo "Unity store is ${UNITY_STORE}"

echo "Trying to move Amazon files from submodule-core-publishing to Assets (skipping on Windows Store)"
if [[ "${UNITY_STORE}" != "WindowsStore" ]] && [[ "${UNITY_STORE}" != "Windows" ]] && [[ "${UNITY_STORE}" != "WSA" ]]; then
    if [[ -d ${amazonCorePath} ]];
    then
        if [[ -d ${amazonAssetsPath} ]];
        then
            echo "${amazonAssetsPath} found, so delete it..."
            rm -rf ${amazonAssetsPath}
        else
            echo "Amazon files not found in Assets"
        fi

        echo "Moving ${amazonCorePath} to ${amazonAssetsPath} started"
        mv ${amazonCorePath} ${amazonAssetsPath}
        echo "Moving finished. Check for result:"
        if [[ -d ${amazonAssetsPath} ]];
        then
            echo "${amazonAssetsPath} was copied"
        else
            echo "${amazonAssetsPath} was NOT copied"
        fi
    else
        echo "Amazon files not found in submodule-core-publishing"
    fi
else
    echo "UNITY_STORE corresponds to Windows Store → skipping Amazon files moving."
fi

if [[ "${UNITY_STORE}" == "Amazon" ]] || [[ "${UNITY_STORE}" == "Huawei" ]];
then

    echo "Start Amazon/Huawei logic"

    ########## REMOVE FIREBASE FOR AMAZON/HUAWEI BOF ##########
    echo "Trying to remove firebase files from submodule-core-publishing"
    if [[ -d ${firebaseCorePath} ]];
    then
            echo "${firebaseCorePath} found, so delete it..."
            rm -rf ${firebaseCorePath}
    else
        echo "Firebase files not found in submodule-core-publishing"
    fi

    echo "Trying to remove firebase files from Assets"
    if [[ -d ${firebaseAssetsPath} ]];
    then
            echo "${firebaseAssetsPath} found, so delete it..."
            rm -rf ${firebaseAssetsPath}
    else
        echo "Firebase files not found in Assets"
    fi

    echo "Trying to remove firebase files from Plugins"
    if [[ -d ${firebasePluginsPath} ]];
    then
            echo "${firebasePluginsPath} found, so delete it..."
            rm -rf ${firebasePluginsPath}
    else
        echo "Firebase files not found in Plugins"
    fi

    # Remove SDK_FIREBASE define from ProjectSettings.asset
    projectSettingsPath="${WORKSPACE}/ProjectSettings/ProjectSettings.asset"
    echo "Trying to remove SDK_FIREBASE define from ProjectSettings"
    if [[ -f ${projectSettingsPath} ]]; then
        # Remove SDK_FIREBASE from scriptingDefineSymbols (handles both ;SDK_FIREBASE and SDK_FIREBASE;)
        sed -i 's/;SDK_FIREBASE//g; s/SDK_FIREBASE;//g; s/SDK_FIREBASE//g' ${projectSettingsPath}
        echo "SDK_FIREBASE define removed from ProjectSettings.asset"
    else
        echo "ProjectSettings.asset not found at ${projectSettingsPath}"
    fi
    ########## REMOVE FIREBASE FOR AMAZON/HUAWEI EOF ##########

    echo "Trying to move ApplovinPlugins files from submodule-core-publishing to Assets"
    if [[ -d ${applovinCorePath} ]];
    then
        echo "Moving ${applovinCorePath} to ${applovinPluginsPath} started"
        cp -R ${applovinCorePath} ${applovinPluginsPath}
        rm -R ${applovinCorePath}
        rmdir ${applovinCorePath}
        echo "Moving finished."
    else
        echo "Applovin plugin files not found in submodule-core-publishing"
    fi
    
    if [[ -d ${applovinCorePath2} ]];
    then
        echo "Moving ${applovinCorePath2} to ${applovinPluginsPath} started"
        cp -R ${applovinCorePath2} ${applovinPluginsPath}
        rm -R ${applovinCorePath2}
        rmdir ${applovinCorePath2}
        echo "Moving finished."
    else
        echo "Applovin plugin files not found in submodule-core-publishing"
    fi

#    echo "Trying to remove Applovin files from Assets"
#    if [[ -d ${applovinAssetsPath} ]];
#    then
#            echo "${applovinAssetsPath} found, so delete it..."
#            rm -rf ${applovinAssetsPath}
#    else
#        echo "Applovin files not found in Assets"
#    fi

#    echo "Trying to remove Applovin files from Plugins"
#    if [[ -d ${applovinPluginsPath} ]];
#    then
#            echo "${applovinPluginsPath} found, so delete it..."
#            rm -rf ${applovinPluginsPath}
#    else
#        echo "Applovin files not found in Plugins"
#    fi

#    echo "Trying to remove Applovin files from submodule-core"
#    if [[ -d ${applovinCorePath} ]];
#    then
#            echo "${applovinCorePath} found, so delete it..."
#            rm -rf ${applovinCorePath}
#    else
#        echo "Applovin files not found in submodule-core"
#    fi

    echo "Trying to remove GeneralLocalRepo"
    if [[ -d ${generalLocalRepoPath} ]];
    then
            echo "${generalLocalRepoPath} found, so delete it..."
            rm -rf ${generalLocalRepoPath}
    else
        echo "GeneralLocalRepo not found in Assets"
    fi

else

    echo "Start Android logic"
    
    echo "Trying to move ApplovinPlugins files from submodule-core-publishing to Assets"
    if [[ -d ${applovinCorePath} ]];
    then
        echo "Moving ${applovinCorePath} to ${applovinPluginsPath} started"
        cp -R ${applovinCorePath} ${applovinPluginsPath}
        rm -R ${applovinCorePath}
        rmdir ${applovinCorePath}
        echo "Moving finished."
    else
        echo "Applovin plugin files not found in submodule-core-publishing"
    fi
    
    if [[ -d ${applovinCorePath2} ]];
    then
        echo "Moving ${applovinCorePath2} to ${applovinPluginsPath} started"
        cp -R ${applovinCorePath2} ${applovinPluginsPath}
        rm -R ${applovinCorePath2}
        rmdir ${applovinCorePath2}
        echo "Moving finished."
    else
        echo "Applovin plugin files not found in submodule-core-publishing"
    fi
    
    echo "Trying to move Firebase Plugins files from submodule-core-publishing to Assets/Plugins"
        if [[ -d ${firebasePluginsForMovePath} ]];
        then
            echo "Moving ${firebasePluginsForMovePath} to ${firebasePluginsTargetPath} started"
            cp -R ${firebasePluginsForMovePath} ${firebasePluginsTargetPath}
            rm -R ${firebasePluginsForMovePath}
            rmdir ${firebasePluginsForMovePath}
            echo "Moving finished."
        else
            echo "Firebase plugin files not found in submodule-core-publishing"
        fi

    echo "Trying to move firebase files from submodule-core-publishing to Assets"
    if [[ -d ${firebaseCorePath} ]];
    then
        echo "${firebaseCorePath} exists"

        echo "Trying to remove GeneralLocalRepo"
        if [[ -d ${generalLocalRepoPath} ]];
        then
                echo "${generalLocalRepoPath} found, so delete it..."
                rm -rf ${generalLocalRepoPath}
        else
            echo "GeneralLocalRepo not found in Assets"
        fi

        if [[ -d ${firebaseAssetsPath} ]];
        then
            echo "${firebaseAssetsPath} found, so delete it..."
            rm -rf ${firebaseAssetsPath}
        else
            "Firebase files not found in Assets"
        fi

        echo "Moving ${firebaseCorePath} to ${firebaseAssetsPath} started"
        mv ${firebaseCorePath} ${firebaseAssetsPath}
        echo "Moving finished. Check for result:"
        if [[ -d ${firebaseAssetsPath} ]];
        then
            echo "${firebaseAssetsPath} was copied"
        else
            echo "${firebaseAssetsPath} was NOT copied"
        fi
    else
        echo "Firebase files not found in submodule-core-publishing"
    fi

fi
########## FIREBASE EOF ##########

