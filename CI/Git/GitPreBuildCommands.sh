#!/bin/bash
# Script for cleaning repository before pull
# WORKSPACE - absolute path to workspace

cd ${WORKSPACE}
git init
git clean -f
git submodule foreach git clean -f
git submodule foreach git reset --hard