@echo off
pushd %~dp0

set UnityExe="C:\Program Files\Unity\Hub\Editor\2019.2.1f1\Editor\Unity.exe"
set UnityProj="../Aseprite2Unity"
set UnityMethod=Aseprite2Unity.Editor.Deploy.DeployAseprite2Unity

echo Deploying Aseprite2Unity
echo Using Editor: %UnityExe%

start /wait "" %UnityExe% -quit --nographics -batchmode -projectPath %UnityProj% -executeMethod %UnityMethod% -logFile output.log
echo Done!

popd