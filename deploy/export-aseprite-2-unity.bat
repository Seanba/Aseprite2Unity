@echo off
pushd %~dp0

set UnityExe="C:\Program Files\Unity\Hub\Editor\2020.3.29f1\Editor\Unity.exe"
set UnityProj="../Aseprite2Unity"
set UnityMethod=Aseprite2Unity.Editor.Deploy.DeployAseprite2Unity

echo Deploying Aseprite2Unity
echo Using Editor: %UnityExe%

start /wait "" %UnityExe% -quit --nographics -batchmode -projectPath %UnityProj% -executeMethod %UnityMethod% -logFile output.log
echo Done!

popd