# How to Build Tizen project on Visual Studio Team Service

## Table of contents
- [Prerequisite](#prerequisite)
  - [Install Visual Studio Tools for Tizen](#install-visual-studio-tools-for-tizen)
  - [Create Visual Studio Team Services Accounts & Project](#create-visual-studio-team-services-accounts--project)
  - [Install VSTS extension (from marketplace)](#install-vsts-extension-from-marketplace)
  - [Install VSTS extension (from develop channel)](#install-vsts-extension-from-develop-channel)
- [Visual Stuido](#visual-stuido)
  - [Create Tizen Project](#create-tizen-project)
  - [Develop your application](#develop-your-application)
  - [Push your code to VSTS](#push-your-code-to-vsts)
- [Visual Studio Team Service](#visual-studio-team-service)
  - [Install(or update) VSTS extension](#installor-update-vsts-extension)
  - [Create VSTS Project & Push source code](#create-vsts-project--push-source-code)
  - [Write Build definition](#write-build-definition)
- [Tip](#tip)
  - [Copy & Publish Artifact](#copy--publish-artifact)

## Prerequisite
### Install Visual Studio Tools for Tizen
- public site : https://marketplace.visualstudio.com/items?itemName=vs-publisher-1484655.VisualStudioToolsforTizen
  ![VSTSInstallVst4tizen](../image/VSTS_install_vst4tizen.png)

### Create Visual Studio Team Services Accounts & Project
- Create Microsoft Accounts
- Create Visual Studio Team Services Accounts (https://www.visualstudio.com/team-services/)
- Create New Project
- Push Tizen Code to VSTS

### Install VSTS extension (from marketplace)
- Go to marketplace & search tizen (https://marketplace.visualstudio.com/items?itemName=tizen.d45d5e83-ee47-4ffc-abe7-844bcc1640a6)
- Uninstall old version of "Tizen Signing Tool" extension if your team space already have it.(This job will take 10~20sec. DO NOTHING while the uninstallation is progressed!)
  - Visit `https://[YOURTEAMSPACE].visualstudio.com/_admin`
  - Click "Extensions" menu.
  - Click the Icon '...' to uninstall extension 
    ![V S T S Uninstall Vsts Ext](../image/VSTS_uninstall_vsts_ext.png)

- Install new version of "Tizen Signing Tool"
  - Go to develop channel (https://marketplace.visualstudio.com/items?itemName=tizen-sdk.d45d5e83-ee47-4ffc-abe7-844bcc1640a6) and check version info
    ![V S T S Install Vsts Ext](../image/VSTS_install_vsts_ext.png)

  - Click the Install button, select team service account and confirm.
    ![V S T S Install Vsts Ext 1](../image/VSTS_install_vsts_ext_1.png)

  - Check the Installed extension
    - Visit `https://[YOUR TEAM SPACE].visualstudio.com/_admin`.
    - Click "Extensions" menu.
      ![V S T S Install Vsts Ext 2](../image/VSTS_install_vsts_ext_2.png)
 
## Visual Stuido
### Create Tizen Project
- File > Project > Tizen > Blank App (Tizen.NUI)
  ![V S T S Create Tizen Proj](../image/VSTS_create_tizen_proj.png)

### Develop your application
- Build
- Run 
- Debug
- Test 

### Push your code to VSTS
- git commit & push to repository to VSTS team project (more info - https://docs.microsoft.com/en-us/vsts/git/)

## Visual Studio Team Service
### Install(or update) VSTS extension 
  - [Install VSTS extension (from marketplace)](#install-vsts-extension-from-marketplace)

### Create VSTS Project & Push source code
- Click "New Project"  button
  ![V S T S Create Vsts Proj](../image/VSTS_create_vsts_proj.png)

- write project name & push tizen source code 
  ![V S T S Create Vsts Proj 1](../image/VSTS_create_vsts_proj_1.png)
 
 
### Write Build definition
- Create Build definition (Build & Release > Builds > + New )
  ![V S T S Create Build Def 0](../image/VSTS_create_build_def_0.png)

- Click the "Empty process"
  ![V S T S Create Build Def 1](../image/VSTS_create_build_def_1.png)

- Write Build definition name & select agent queue
  ![V S T S Create Build Def 2](../image/VSTS_create_build_def_2.png)

- Select source repository & branch 
  ![V S T S Create Build Def 3](../image/VSTS_create_build_def_3.png)

- Click "+" Icon to add task > Add .NET Core Task 
  ![V S T S Create Build Def 4](../image/VSTS_create_build_def_4.png)

- Select project file to build on Project field (**/*.csproj)
  ![V S T S Create Build Def 5](../image/VSTS_create_build_def_5.png)

- Click "+" Icon to add task > Add Tizen Signing Tool Task (if you can't see tizen task. please refer Install VSTS extension)
  ![V S T S Create Build Def 6](../image/VSTS_create_build_def_6.png)

- Select Identifiers > click icon to upload certificate (Author Certificate & Distributor Certificate)
  ![V S T S Create Build Def 7](../image/VSTS_create_build_def_7.png)
  ![V S T S Create Build Def 8](../image/VSTS_create_build_def_8.png)

- Variables Tab > add password Key & Value > Click lock icon
  ![V S T S Create Build Def 9](../image/VSTS_create_build_def_9.png)

- Tasks Tab Add Password variables 
  ![V S T S Create Build Def 10](../image/VSTS_create_build_def_10.png)

- Click Save & queue > See the build process 
  ![V S T S Create Build Def 11](../image/VSTS_create_build_def_11.png)
 
## Tip
### Copy & Publish Artifact 
- Add Copy Files Task (https://docs.microsoft.com/en-us/vsts/build-release/tasks/utility/copy-files)
- Set the Contents Field (ex : **/*.tpk)
- Set the Target Folder (ex : tpkresult)
  ![V S T S Tip Copy Publish 0](../image/VSTS_Tip_copy_publish_0.png)

- Add Publish Artififact Task (https://docs.microsoft.com/en-us/vsts/build-release/tasks/utility/publish-build-artifacts)
- Set the Path to publish (ex : tpkresult)
- Set the Artifact name (ex : tpk)
  ![V S T S Tip Copy Publish 1](../image/VSTS_Tip_copy_publish_1.png)

- Save & queue
- Go to Build Result View 
  ![V S T S Tip Copy Publish 2](../image/VSTS_Tip_copy_publish_2.png)

- Click artifact tab & click explore 
  ![V S T S Tip Copy Publish 3](../image/VSTS_Tip_copy_publish_3.png)
