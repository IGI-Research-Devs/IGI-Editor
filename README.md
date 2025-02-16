# Project I.G.I Editor

[Project I.G.I](https://en.wikipedia.org/wiki/Project_I.G.I.) Editor is all in one game editor which lets you edit all game objects including _Buildings/3D/AI/Weapons_ and lets you to design your own _custom level_ in the game and lets you _upload/download_ your level to server and share with your friends.

## Pre-Requisite
- **General section.**
- [DLL File](https://en.wikipedia.org/wiki/Dynamic-link_library) - This project uses [IGI Natives.dll](https://github.com/IGI-Research-Devs/IGI_Internal/) file and inject that into Game to call game native methods. .</br>
- [DLL Injection](https://en.wikipedia.org/wiki/DLL_injection) - This project needs DLL injection into _IGI_ game.</br>
- **Game specific section.**
- [IGI Graphs Structure](https://github.com/IGI-Research-Devs/IGI-Research-Data/blob/main/Research/GRAPH/Graph-Structure.txt) - Project IGI 1 Graph structure data.
- [IGI 3D Models](https://github.com/IGI-Research-Devs/IGI-Research-Data/blob/main/Research/Natives/IGI-Models.txt) - Project IGI uses 3D models in 
form of _MEF_ (**M**esh **E**xternal **F**ile).
- [IGI Camera View](https://www.researchgate.net/figure/Definition-of-pitch-roll-and-yaw-angle-for-camera-state-estimation_fig15_273225757) - IGI use game Camera called [Viewport](https://en.wikipedia.org/wiki/Viewport) to display the game.

## Editor components.
Editor has several components which it needs in order to work fully to its functionality.
- Server : Located at [OrgFree](http://igiresearchdevelopers.orgfree.com/) which contains Mission files and Resources like Weapon/A.I images for editor.
- ~~Database : Located at Github Gist private repo contains privacy information like _I.P,Mac Address_ and _Key_ for Editor.~~
- Internal DLL : Located at [IGI-Internals](https://github.com/IGI-Research-Devs/IGI_Internal) is used to call IGI Game internal native methods.
- QEditor - Located at path `C:\Users\my_username\AppData\Roaming\QEditor`  This is appdata file comes pre-installed with any version of editor and can be downloaded from here [QEditor Full Version](https://cutt.ly/p1ASiQX).</br>

## QEditor components.
**Q**Editor was the initial name of the project because editor actually modifies **Q** files of games like _QVM,QSC,QAS_ files but later it was changed.</br>

    .
    ├── AIFiles                 # AI Script and Path files.
    ├── QCompiler               # Compiler/Decompiler for IGI.
    ├── QFiles                  # Source files - QSC, Binary files - QVM.
    ├── QGraphs                 # Graphs files and areas.
    ├── QMissions               # Custom Missions files.
    ├── QWeapons                # Custom Weapons files.
    ├── Void                    # Empty Mission files.
    ├── aiIdle.qvm              # File for setting AI to idle state.
    ├── IGIModels.txt           # Contains 3D Models information.
    ├── keywords.txt            # Keywords for QVM Editor.
    ├── QChecks.dat             # Contain MD5 Hashes to check file integrity.
    └── weaponconfig.qvm        # Weapon config file.


## Editor Working flow.
Editor most of the time just compiles the script file _QSC_ called _Q_ script source into _QVM_ called _Q_ virtual machine and Restart the level in order to see the affected changes. 
Here is workflow mentioned.
1. Get game level and select proper source file of level located at `QEditor\QFiles\IGI_QSC\missions\location0\level`.</br>
2. Updates the script file with new script depends upon logic Add/Update/Delete commands.</br>
**Internal compiler**</br>
3. Copies script file from Editor `C:\IGI-Editor` to `D:\IGI\` and compiles them to binary _QVM_ file and moves them to proper destination.</br>
**External compiler**</br>
3. Copies script file from Editor `C:\IGI-Editor` to `QEditor\QCompiler\Compile\input` and compiles them to binary _QVM_ file and convert compiled QVM v7 (IGI 2) to QVM v5 (IGI 1) using _DConv_ tool and moves them to proper destination.</br>
4. Restart the level and see the changes.</br>

## Editor sections.
### Level Editor:
**Level Editor**: Lets you to `Add` or `Remove` Building/Objects in level at any position you want select list of objects to add in level and add then in `Edit mode`.</br>

![](https://github.com/IGI-Research-Devs/IGI-Editor/blob/master/resources/level_editor.png)

### Human Editor:
**Human Editor**: Lets you to Update human Speed/Jump,Health Scale peek and Team Id/Human Camera (1st Person,3rd person).</br>

![](https://github.com/IGI-Research-Devs/IGI-Editor/blob/master/resources/human_editor.png)

### Weapon Editor:
**Weapon Editor**: Lets you to `Add` or `Remove` new weapons in level `permanently` or `temporary` depends which mode is selected `Live` or `Normal` mode.</br>

![](https://github.com/IGI-Research-Devs/IGI-Editor/blob/master/resources/weapon_editor.png)

### Weapon Advance Editor:
**Weapon Advance Editor**: Lets you to `Update` Weapon's advance data like _Name,UI Type,Ammo Damage, Weapon Power/RPM and more_.</br>

![](https://github.com/IGI-Research-Devs/IGI-Editor/blob/master/resources/weapon_advance_editor.png)

### A.I Editor:
**A.I Editor**: Lets you to `Add` or `Remove` `Friendly/Enemy` A.I into level with various properties like _invincible_,_advance view_,_guard generator_ etc.</br>

![](https://github.com/IGI-Research-Devs/IGI-Editor/blob/master/resources/ai__editor.png)

### A.I JSON Editor.
**JSON Editor**: Lets you to `Save` or `Load` A.I to `JSON` file _permanently_ for later use, and you can edit/share json files into editor.</br>
![](https://github.com/IGI-Research-Devs/IGI-Editor/blob/master/resources/ai_json_editor.png)


### Mission Editor
**Mission Editor**: Lets you to `Add` or `Remove` `Missions` into level after you design your own custom level enter _Name_ and _Description_ and **Save** your Mission</br>
You can also **Upload** or **Download** new missions from _Server_ and load into your game and play them directly..</br>

![](https://github.com/IGI-Research-Devs/IGI-Editor/blob/master/resources/mission_editor.png)

### Position Editor
**Position Editor**: Lets you to `Update` or `Reset` `Positions` and `Orientation` of Game objects and Humanplayer you can Rotate them to `180` degree or move them to different location easily.</br>

![](https://github.com/IGI-Research-Devs/IGI-Editor/blob/master/resources/position_editor.png)


### Texture Editor
**Texture Editor**: Lets you to `View` and `Replace` `Textures` of game easily with inbuilt custom tools now you can view all game textures and replace them with your own Textures in `.JPG/.PNG` files .</br>
- This can automatically Import textures from `.RES` Resource packed file annd view them directly into editor.</br>
- This can automatically Replace Images from `JPG/PNG` to `TGA` Game supported format and turn that into game texture easily.</br>

![](https://github.com/IGI-Research-Devs/IGI-Editor/blob/master/resources/texture_editor.png)

### Graph Editor.</br> 
**Graph editor**: Lets you see visualization of Graph and nodes information, You can `teleport` or `Auto traverse` to Graph or Nodes in _real time_ and see where graph or nodes are in selected level..</br>

![](https://github.com/IGI-Research-Devs/IGI-Editor/blob/master/resources/graph_editor.png)

### Graph & Links - Level 5. ( IGI 1 Editor).
![](https://i.ibb.co/px7fWfS/Node-Links-L5-Area2.png)

### Graph & Links - Level 1. ( IGI 2 Editor).
![](https://i.ibb.co/gR1vZSM/IGI2-Graph-Nodes.png)

### Misc Editor:
**Misc editor**: Lets you to `Reset/Remove` levels back to normal and `Remove Cutscenes`, Export level data,Set Game Frames `FPS` Update Game Music and more.</br>

### IGI Editor License Key:
~~This editor requires _license key_ to run contact via _email_ or _discord_ and mention your **Username** to identify yourself.</br>
And now you can generate Key from KeyGen for your editor for free.</br>~~

## Future version includes:
* All **new UI/UX Design** with More **advanced editor**.
* **Real Time Editor Support**.
* **Resource/Profile/GameConfig/QCompiler** editor.
* **Compiler** and **Assembler** with _Parser_ output like **.qsc** to **.qas** to **.qvm**.

### Profile Editor: **_Available in Future version_**.</br> 
**Profile editor**: Lets you manage your `Game Profiles` you can update _Name,Level,Mission_ details or  `Add` or `Remove` new player profile for your player.</br>

### Resource Editor: **_Available in Future version_**.</br> 
**Resource editor**: Lets you see all details of Game resources information like Resource _Name,Address,Id_ and can `Load` or `Unload` any resource in _Real time_.</br>

### GameConfig Editor: **_Available in Future version_**.</br> 
**GameConfig editor**: Lets you edit game configuration like _Level,Password,ActiveMission,Graphics/Music/Mouse settings,Game Key controls,Game difficulty_ and more.</br> 

### QCompiler Editor: **_Available in Future version_**.</br> 
**QCompiler editor**: Lets you `Compile/Assemble/Parse` game files which game uses internaly to save/edit game data.</br>

# Editor Tutorial on YouTube :
[![QEditor](https://img.youtube.com/vi/wMyAlgIm2AY/0.jpg)](https://www.youtube.com/watch?v=wMyAlgIm2AY)

## Version Update:
**Editor version 0.8.5.1 Latest.**

# Chagelogs:
Check out the detailed version history in [Changelogs.md](CHANGELOGS.md)

## Privacy Policy:
The editor onwards version 0.7 doesn't store any type of data from _User,Machine_ the editor doesn't maintain any sort of database now.

# **DOWNLOAD LINKS**</br>
- **Project I.G.I 1 Editor** Version 0.8.5.1 _RELEASED_</br>
[IGI Editor](https://github.com/IGI-Research-Devs/IGI-Editor/releases/tag/0.8.5.1)</br>

# 📚 FAQ (Frequently Asked Questions)
Checkout the detailed FAQ in [FAQ.md](FAQ.md)

# 📞 Connect with us:
If you encounter any issues with the Editor, don't hesitate to contact me 👇

- 🎮 Discord: Feel free to message me at _Jones_IGI#3954_ and join our [Discord server](https://discord.gg/AyVDW7kE6V) for quick support.
- 📧 Email: You can reach me at igiproz.hm@gmail.com for any questions or feedback.
- 🌟 Follow the Project: Stay updated with the latest developments on [GitHub](https://github.com/IGI-Research-Devs/).
- 📺 Subscribe to our Channel: Watch useful guides and walkthroughs on our [YouTube](https://www.youtube.com/@igi-research-devs) channel.

👤 Original Author: _HeavenHM@2022_.
