Plays animated gifs (videos) on the lcd screens. 

Note: Requires Space Engineers version 1.161.005 or higher. It depends on the new monospace font with colors that was just added. 

#Quick Demonstration Video:
https://youtu.be/0wFoN6xMMAc

#Instructions
/************************************************************************************************************ 
SpaceAnimatedGifs - A vanilla in-game script for playing animated gifs on LCDs 

LCD Setup 

Change the font size to the smallest, and change the font to the new "monospace" one. Change the lcdName 
setting below to the name of your lcd. 

How to load an animated gif into the game. 

1. Go to http://spaceengineers.io/tools/storage-loader 
2. Drag your animated gif file into the gray box on that page. (Try to use small GIFs, large ones will take a lot of work to import this way :-). It basically splits the file up into 100k chunks) 
3. Depending on the size of the gif, it could take a little while to load. 
4. Copy each script under the file name on that page into the programming block in game and run the PB once for each one. (NOTE: Make sure you don't have a timer running the PB while your are doing this. If the script runs more than once for any of the storage loader scripts, it won't work) Make sure you run them in order. 
5. Load this script into the PB and start a timer running the PB and then triggering itself. 
6. Wait. After a little while (can take up to a few minutes depending on how big the gif is) the gif will 
start playing. 

Note: Adjust the throttle below (in the script) if you get complexity errors. 

Fork on github: http://github.com/rockyjvec/SpaceAnimatedGifs 
*************************************************************************************************************/ 

The complex part of the script is loading the gif at the beginning. Once it loads and starts playing it actually uses very little cpu because each frame is stored as the text which is just dumped into the LCD public text. Thanks to everyone who worked to add the monospace font with color pixels to the game. :-) 
