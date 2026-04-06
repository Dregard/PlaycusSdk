#!/bin/bash
# Script for upload WebGL application to S3 bucket
# Environment variables from Ci system:
# WORKSPACE - absolute path to workspace
# DESTINATION - RELEASE / development / appcenter etc
# CLOUDFRONT_DISTRIBUTION_ID - aws CloudFront distribution id

if [ -d "${WORKSPACE}/Build/ServerData" ];
then

    cd ${WORKSPACE}/Build/ServerData

    S3BUCKET=playcuscdn-dev

    if [[ "$DESTINATION" == "RELEASE" ]];
    then
      S3BUCKET=playcuscdn
    fi

    echo 'DESTINATION='$DESTINATION
    echo 'S3BUCKET='${S3BUCKET}

    aws s3 sync . s3://${S3BUCKET}/

    if [[ "$DESTINATION" != "RELEASE" ]];
    then
        firstDirPath=$(ls -d */|head -n 1)
        echo 'firstDirPath = '$firstDirPath
        aws cloudfront create-invalidation --distribution-id ${CLOUDFRONT_DISTRIBUTION_ID} --paths "/$firstDirPath*"
    fi

else
    echo "Error: Directory ${WORKSPACE}/Build/ServerData does not exists."
fi
