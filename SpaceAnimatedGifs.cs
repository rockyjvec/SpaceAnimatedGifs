/************************************************************************************************************ 
    SpaceAnimagedGifs - A vanilla in-game script for playing animated gifs on LCDs 
     
    How to load an animated gif into the game. 
     
    1.  Go to http://spaceengineers.io/tools/storage-loader 
    2.  Drag your animated gif file into the gray box on that page. 
    3.  Depending on the size of the gif, it could take a little while to load. 
    4.  Copy each script under the file name on that page into the programming block in game and run the PB 
        once for each one.  (NOTE: Make sure you don't have a timer running the PB while your are doing this. 
        If the script runs more than once for any of the storage loader scripts, it won't work) 
    5.  Load this script into the PB and run it.
    6.  Wait.  After a little while (can take up to a few minutes depending on how big the gif is) the gif will 
        start playing. Some GIFs may not work.
         
    Note: Adjust the throttle below if you get complexity errors. 
    
    You can use programming block arguments to change which frame(s) are played.  Ex: "frames 1 3 5 7" will 
    play frames 1, 3, 5 and 7, then stop.  If you add loop into the list of frames, it will play each frame until the
loop and then loop the remaining frames. Ex: "frames 1 2 3 4 5 loop 6 7 8 9 10"
        
    Fork on github: http://github.com/rockyjvec/SpaceAnimatedGifs 
*************************************************************************************************************/ 
 
int frameWidth = 356; // width of the lcd (178 for small LCDs)
int frameHeight = 178; // height of the lcd
 
// For a video wall, list left to right, top to bottom. Like:
// [1][2]
// [3][4]
string[] lcdNames = new string[] {"GIFPlayer"}; // list of LCDs to broadcast to

// If true, multiple LCDs will be treated as one big screen.  Must have same number of LCDs tall as wide.
bool wall = false; 

bool loop = true;

List<int> frames = new List<int>{}; // customize which frames play.  Ex: List<int> frames = new List<int>{1,3,5,7};

bool cache = false; // permanently cache the frames from the GIF so it doesn't need to be decoded every time.

int frameSkip = 0; // skips every frameSkip frames to increase performance.

int throttle = 1700; // Set this lower to prevent complexity errors.  Set it higher to decrease loading times. 
/****************************************************************************



















*****************************************************************************/
public Program() 
{ 
    Runtime.UpdateFrequency = UpdateFrequency.Update1;
    foreach(string lcdName in lcdNames)
    {
        List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
        GridTerminalSystem.SearchBlocksOfName(lcdName, blocks);
        
        if(blocks.Count > 0)
        {
            foreach(IMyTextPanel screen in blocks)
            {
                if(screen.CustomName != lcdName) continue;
                
                float fontSize = 0.1f;
                
                if(wall) fontSize = fontSize * (float)Math.Sqrt((double)lcdNames.Length);
                
                screen.ShowPublicTextOnScreen();
                screen.SetValue("FontSize", fontSize);
                screen.SetValue<long>("Font", 1147350002);
                screens.Add(screen);
                
                screen.WritePublicText("", false);
            }
        }
        else
        {
            throw new Exception("LCD named \"" + lcdName + "\" not found.");
        }
    }
    gif = new Gif(frameWidth, frameHeight, Storage);
    Gif.Ech = Echo;
    Decoder.Ech = Echo;
} 

void Main (string args) 
{
    if(args.IndexOf("frames") == 0)
    {
        loop = false;
        frame = 0;
        frames.Clear();
        string[] frameNumbers = args.Split(' ');
        foreach (string number in frameNumbers)
        {
            if(number != "frames")
            {
                if(number == "loop")
                {
                    frames.Add(-1);
                }
                else
                {
                    string[] range = number.Split('-');
                    
                    if(range.Length == 1)
                    {
                        frames.Add(Int32.Parse(number));                        
                    }
                    else if(range.Length == 2)
                    {
                        int min = Int32.Parse(range[0]);
                        int max = Int32.Parse(range[1]);
                        
                        for(int r = min; r <= max && max >= min; r++)
                        {
                            frames.Add(r);
                        }
                    }
                }
            }
        }
        
        Echo("Frames updated.");
    }
    
    if(args == "toggleWall")
    {
        wall = !wall;
        foreach(IMyTextPanel screen in screens)
        {
            float fontSize = 0.1f;            
            if(wall) fontSize = fontSize * (float)Math.Sqrt((double)lcdNames.Length);
            screen.SetValue("FontSize", fontSize);
        }
    }
    
    if(running) 
    { 
        int count = 0;
        while ((gif.maxSteps == 0 || count < gif.maxSteps) && count++ < throttle) 
        { 
            try
            {                
                if(!gif.step()) 
                { 
                    if(Storage[0] != (char)'|') 
                    {
                        if(cache)
                        {
                            Storage = gif.serialize();
                        }
                        Echo(gif.frames.Count + " Frames loaded from GIF ("+ gif.totalFrames+")."); 
                    } 
                    else 
                    { 
                        Echo(gif.frames.Count + " Frames loaded from cache.");                     
                    } 
                     
                    running = false; 
                    break; 
                } 
            }
            catch(Exception e)
            {
                Echo($"Exception: {e}\n---");
                throw;
            }
        } 
        return; 
    }
     
    int currentFrame = frame;
    if(frames.Count > 0)
    {
        if(frame >= frames.Count)
        {
            if(loop) frame = 0;
            else frame = frames.Count - 1;
        }
        if(frames[frame] == -1) // loop
        {
            frames.RemoveRange(0, frame + 1);
            frame = 0;
            loop = true;
        }
        currentFrame = frames[frame];
    }

    long now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
    if(now - lastFrame >=  delay * 10) 
    {        
        bool draw = false;
        if(lastFrame > 0 && ((now - lastFrame) - (delay * 10) < 100))
        {
            lastFrame = now - ((now - lastFrame) - (delay * 10));
            draw = true;
        }
        else
        {
            if(lastFrame == 0)
            {
                draw = true;
            }
            lastFrame = now; 
        }
        if(draw && (frameSkip == 0 || (frame % frameSkip != 0)))
        {
            /** Send the frame to the LCD(s) **/
            if(wall)
            {
                int size = (int)Math.Sqrt((double)screens.Count);
                int wide = frameWidth / size;
                int tall = frameHeight / size;
                for(int y = 0; y < size; y++)
                {
                    int yOffset = y * tall * (frameWidth + 1);
                    for(int x = 0; x < size; x++)
                    {
                        string output = "";
                        int xOffset = yOffset + x * wide;
                        for(int line = 0; line < frameHeight / size; line++)
                        {
                            output += (new String(gif.frames[currentFrame % gif.frames.Count], xOffset + line * (frameWidth + 1), wide)) + "\n";
                        }
                        screens[y*size+x].WritePublicText(output, false);
                    }
                }
            }                
            else
            {
                foreach(IMyTextPanel screen in screens)
                {
                    screen.WritePublicText(new String(gif.frames[currentFrame % gif.frames.Count]), false);
                }
            }
        }
        delay = gif.delays[currentFrame % gif.frames.Count];
        if(delay < 3) delay = 10; // this is how most browsers handle 0
        frame++;
    } 
} 

public class Decoder 
{ 
    bool debug = false;
    
    public byte[] pixels;
    int currentPixel = 0;
    
    Dictionary<int,byte[]> codeTable;

    int minCodeSize; 
    int codeSize;
 
    int code; 
    int previousCode = -1;
    int clearCode;
    int endCode;
    int nextCode;
  
 
    BitArray buffer;
    int bitIndex = 0;
 
    public static Action<string> Ech;
    
    public void Echo(string e)
    {
        if(debug) Ech(e);
    }
    
    public Decoder(int width, int height, byte[] buffer, int minCodeSize) 
    { 
        this.codeTable = new Dictionary<int,byte[]>();
        this.buffer = new BitArray(buffer); 
        this.minCodeSize = minCodeSize; 
 
        this.codeSize = minCodeSize + 1;
        this.clearCode = 1 << minCodeSize; // 2 ^ minCodeSize
        this.endCode = clearCode + 1;
        this.nextCode = endCode + 1;
 
        this.pixels = new byte[width*height]; 
    } 
 
    public bool decode() 
    {
        previousCode = code;
        
        // get code from buffer bitstream
        code = 0;
        for(int bit = 0; bit < codeSize; bit++)
        {
            if(buffer[bitIndex+bit])
            {
                code += 1 << ( bit);
            }
        }
        Echo($"CODE: {code}");
        bitIndex += codeSize;
        if(code == clearCode)
        {
            // initialize codeTable
            codeSize = minCodeSize + 1;
            codeTable.Clear();
            nextCode = endCode + 1;
            for (code = 0; code < clearCode + 2; code++) 
            {
                codeTable.Add((int)code, new byte[1]{(byte)code});
            } 
        }
        else if(code == endCode)
        {
            return false; // stop looping, done.
        }
        else
        {           
            byte[] previous;
            byte[] newRow;
            byte[] row;
            
            if(codeTable.TryGetValue(code, out row)) // it was in the table
            {
                Echo("In"+previousCode);
                Array.Copy(row, 0, pixels, currentPixel, row.Length);
                currentPixel += row.Length;
                
                if(previousCode != clearCode && codeTable.TryGetValue((int)previousCode, out previous))
                {
                    newRow = new byte[previous.Length + 1];
                    Array.Copy(previous, 0, newRow, 0, previous.Length);
                    newRow[previous.Length] = row[0];
                    if(nextCode < 4096)
                    {                        
                        Echo($"Adding {nextCode} " + String.Join(",",newRow));
                        codeTable.Add((int)nextCode++, newRow);

                        if(nextCode > (1 << codeSize) - 1 && codeSize < 12) 
                        {
                            codeSize++;
                        }
                    }
                }
            }
            else
            {
                Echo("Out");

                if(previousCode != clearCode && codeTable.TryGetValue((int)previousCode, out previous))
                {
                    newRow = new byte[previous.Length + 1];
                    Array.Copy(previous, 0, newRow, 0, previous.Length);
                    newRow[previous.Length] = previous[0];
                    
                    if(nextCode < 4096)
                    {
                        Echo($"Adding {nextCode} " + String.Join(",",newRow));
                        codeTable.Add((int)nextCode++, newRow);

                        if(nextCode > (1 << codeSize) - 1 && codeSize < 12) 
                        {
                            codeSize++;                            
                        }
                    }
                    
                    Array.Copy(newRow, 0, pixels, currentPixel, newRow.Length);
                    currentPixel += newRow.Length;
                }

            }           
        }
        
        previousCode = code;

        return true; // continue looping.
    } 
 
} 
 
public class Gif 
{ 
    bool debug = false;
    
    Decoder decoder;
    
    public int maxSteps = 0;
 
    // lSD    
    public int width; 
    public int height; 
 
    public int LCDwidth = 178; 
    public int LCDheight = 178; 
 
    int globalColorTableSize; 
    byte[][] globalColorTable; 
    int localColorTableSize; 
    byte[][] localColorTable; 
    byte[] data; 
    long counter = 0; 
    byte backgroundColor; 
//    bool gce = false; 
    int lzwMinimumCodeSize; 
    byte[] lzwData; 
    int lzwDataIndex = 0; 
    public Func<bool> step; 
//    long decodeBit = 0; 
    byte[] output; 
    int top, left, w, h; 
    bool interlaceFlag; 
    int x, y; 
    int transparent = 0; 
    bool is_transparent = false; 
    bool restore_background = false; 
    bool do_not_dispose = false; 
    bool last_do_not_dispose = false;
    
    
    int[,] floydSteinberg;
 
    char[] frame, last; 
    public List<char[]> frames = new List<char[]>(); 
    public List<int> delays = new List<int>(); 
    public int totalFrames = 0;
    
    public static Action<string> Ech;
    public void Echo(string e)
    {
        if(debug) Ech(e);
    }
    
    int error = 0;
    
    char getColor(int r, int g, int b, int index)
    {
        int rresult = (r+error+floydSteinberg[index % (LCDwidth +1),0]) * 7 / 255;
        int rback = rresult * 255 / 7;
        int rerror = r - rback;
        if(rresult > 7) rresult = 7;
        if(rresult < 0) rresult = 0;

        int gresult = (g+floydSteinberg[index % (LCDwidth +1),1]) * 7 / 255;
        int gback = gresult * 255 / 7;
        int gerror = g - gback;
        if(gresult > 7) gresult = 7;
        if(gresult < 0) gresult = 0;

        int bresult = (b+floydSteinberg[index % (LCDwidth +1),2]) * 7 / 255;
        int bback = bresult * 255 / 7;
        int berror = b - bback;
        if(bresult > 7) bresult = 7;
        if(bresult < 0) bresult = 0;

        floydSteinberg[(index + 1) % (LCDwidth + 1),0] = rerror * 7 / 16;
        floydSteinberg[(index + 1) % (LCDwidth + 1),1] = gerror * 7 / 16;
        floydSteinberg[(index + 1) % (LCDwidth + 1),2] = berror * 7 / 16;

        floydSteinberg[(index + LCDwidth - 1) % (LCDwidth + 1),0] = rerror * 3 / 16;
        floydSteinberg[(index + LCDwidth - 1) % (LCDwidth + 1),1] = gerror * 3 / 16;
        floydSteinberg[(index + LCDwidth - 1) % (LCDwidth + 1),2] = berror * 3 / 16;

        floydSteinberg[(index + LCDwidth) % (LCDwidth + 1),0] = rerror * 5 / 16;
        floydSteinberg[(index + LCDwidth) % (LCDwidth + 1),1] = gerror * 5 / 16;
        floydSteinberg[(index + LCDwidth) % (LCDwidth + 1),2] = berror * 5 / 16;

        floydSteinberg[(index + LCDwidth + 1) % (LCDwidth + 1),0] = rerror * 1 / 16;
        floydSteinberg[(index + LCDwidth + 1) % (LCDwidth + 1),1] = gerror * 1 / 16;
        floydSteinberg[(index + LCDwidth + 1) % (LCDwidth + 1),2] = berror * 1 / 16;
        
        return (char)('\uE100' + bresult + gresult * 8 + rresult * 64);
    }
 
    bool createFrame() 
    { 
        byte[] color = localColorTable[backgroundColor]; 
        byte[] bgcolor = localColorTable[backgroundColor]; 
         
        float scale = 1; 
        if (width > height) 
        { 
            scale = (float)((float)width / (float)LCDwidth); 
        } 
        else 
        { 
            scale = (float)((float)height / (float)LCDheight); 
        } 
         
        int sx = (int)(x * scale); 
        int sy = (int)(y * scale); 
 
         
        bool draw = false; 
        if (sx >= left && sx < left + w && sy >= top && sy < top + h) 
        { 
            int spot = ((sy - top) * (w)) + (sx - left); 
            if (spot < output.Length) 
            { 
                draw = true; 
                byte index = output[spot]; 
                color = localColorTable[index]; 
                if (this.is_transparent && index == transparent)  
                { 
                    draw = false; 
                } 
            } 
        } 
 
        if(this.restore_background && !draw)
        {
            frame[x + ((LCDwidth + 1) * y)] = getColor(bgcolor[0], bgcolor[1], bgcolor[2], y * LCDwidth + x);
        }
  
        if (draw || frame[x + ((LCDwidth + 1) * y)] < '\uE100')
        {
            frame[x + ((LCDwidth + 1) * y)] = getColor(color[0], color[1], color[2], y * LCDwidth + x); 
        }
              
        x += 1; 
         
        if (x > LCDwidth - 1) 
        { 
            frame[x + ((LCDwidth + 1) * y)] = "\n"[0]; 
            x = 0; 
            y++; 
            if (y > (LCDheight - 1)) 
            { 
                frames.Add(frame);
                Ech("Frame " + frames.Count + " added\ntransparent: " + is_transparent + "\nrestore_background: " + restore_background + "\ndo_not_dispose: " + do_not_dispose + " last_do_not_dispose: " + last_do_not_dispose);
//                if(frames.Count > 9) return false;
                // reset state?
                is_transparent = false; 
                restore_background = false; 
                do_not_dispose = false; 
                last_do_not_dispose = false; 
                
                step = mainLoop; 
                y = 0; 
                return true; 
            } 
        } 
 
        return true; 
    }
    
    bool decodeFinish()
    {
        maxSteps = 0;
        this.output = this.decoder.pixels; 
        x = 0; 
        y = 0; 


        if (last == null) last = new char[(LCDwidth + 1) * LCDheight]; 
        if (frame != null) last = frame;
        frame = new char[(LCDwidth + 1) * LCDheight]; 
        Array.Copy(last, frame, frame.Length); 
       
        step = createFrame; 
        return true; 
    }
 
    bool decode() 
    { 
        maxSteps = 2000;
        if (this.decoder.decode()) 
        {
            return true; 
        } 
        else 
        {
            maxSteps = 1;
            step = decodeFinish;
            return true;
        } 
    } 
 
    bool decodeStart() 
    { 
        this.decoder = new Decoder(w, h, lzwData, lzwMinimumCodeSize);
        step = decode;
        return true; 
    } 
 
    bool getLzwData() 
    {       
        maxSteps = 0;
        int len = data[counter++];
        Array.Copy(data, counter, lzwData, lzwDataIndex, len);
        lzwDataIndex += len;
        counter += len;
 
        if (data[counter] == 00) 
        { 
            maxSteps = 1;
            Echo("Starting decode.");
            counter++; 
            step = decodeStart; 
        } 
        return true; 
    } 
/* 
    bool applicationExtension() 
    { 
        counter += data[counter++]; // skip application block
        step = applicationExtensionSubBlockLoop;
        return true; 
    } 
  */  
    bool applicationExtensionSubBlockLoop()
    {
        if (data[counter++] == 0x00)
        {
            Echo("    exiting application extension SubBlock loop");
            step = mainLoop; 
        }
        else
        {
            counter += data[counter-1]; // skip data sub-block
        }
        return true;
    }
    
 
    bool mainLoop() 
    { 
        if (counter > data.Length)
        {
            throw new Exception("something strange happened, GIF ended early!");            
        }

        //gce = false; // had graphics control extension    
        switch (data[counter++]) 
        { 
            case 0x21: // extension    
                Echo("Extension");
                switch (data[counter++]) // which extension is it?
                { 
                    case 0xF9: // Graphics control extension    
                        Echo("  graphics control");
                        //gce = true; 
                        if(data[counter++] != 0x04) // Block size 0x04 
                        {
                            throw new Exception("Error in graphics control extension. Invalid block size!");
                        }
                        
                        int flags = data[counter++]; // Flags 

                        // 1 -   Transparent flag
                        this.is_transparent = (flags & 0x1) > 0; 

                        // 2 -   User Input flag (unused)
                        
                        // 3-5 - Disposal Method

                        // 3.0 -   No disposal specified. The decoder is 
                        //       not required to take any action. 
                        // 3.1 -   Do not dispose. The graphic is to be left 
                        //       in place. 
                        this.last_do_not_dispose = this.do_not_dispose; 
                        this.do_not_dispose = (flags & 0x4) > 0; 
                         
                        // 3.2 -   Restore to background color. The area used by the 
                        //       graphic must be restored to the background color. 
                        this.restore_background = (flags & 0x8) > 0; 
                         
                        // 3.3 -   Restore to previous. The decoder is required to 
                        //       restore the area overwritten by the graphic with 
                        //       what was there prior to rendering the graphic. 
 
                        // 4-7 -    To be defined. 
                        this.delays.Add(data[counter++] | (data[counter++] << 8)); // Delay Time 
                        transparent = data[counter++]; // Transparent Color Index 
                        if(data[counter++] != 0x00) // Block Terminator (0x00) 
                        {
                            throw new Exception("Error in graphics control extension.  Block terminator not found!");
                        }
                        break; 
                    case 0xFF: // Application extension
                        Echo("  application");
//                        step = applicationExtension; 
                        step = applicationExtensionSubBlockLoop;
                        break;
                    case 0xFE: // Comment extension
                        Echo("  comment");
                        step = applicationExtensionSubBlockLoop;
                        break;
                    case 0x01: // Plain text extension
                        Echo("  plain text");
  //                      step = applicationExtension; 
                        step = applicationExtensionSubBlockLoop;
                        break;
                    default:  // Unknown extension
                        Echo("  unknown" + data[counter-1]);
                        step = applicationExtensionSubBlockLoop;
                        break;
                } 
                break; 
            case 0x2c: // image descriptor 
                if(maxSteps == 0)
                {
                    maxSteps = 1;
                    counter--;
                    return true;
                }
                
                Echo("ID");
                totalFrames++;

                left = data[counter++] | (data[counter++] << 8); 
                top = data[counter++] | (data[counter++] << 8); 
 
                w = data[counter++] | (data[counter++] << 8); 
                h = data[counter++] | (data[counter++] << 8);
                
                bool localColorTableFlag = (data[counter] & 0x80) > 0; 
                interlaceFlag = (data[counter] & 0x40) > 0; 
                bool sortFlag = (data[counter] & 0x20) > 0; 
                localColorTableSize = (int)Math.Pow(2, (((data[counter] & 0x07)) + 1)); 
                counter++; // skip packed field used above    
 
                if (localColorTableFlag) 
                {
                    localColorTable = new byte[localColorTableSize][]; 
                    for (int i = 0; i < localColorTableSize; i++) 
                    { 
                        localColorTable[i] = new byte[3]; 
                    } 
                    for (int i = 0; i < localColorTableSize; i++) 
                    { 
                        localColorTable[i][0] = data[counter++]; 
                        localColorTable[i][1] = data[counter++]; 
                        localColorTable[i][2] = data[counter++]; 
                    } 
                } 
                else 
                { 
                    localColorTableSize = globalColorTableSize; 
                    localColorTable = globalColorTable; 
                } 
 
                lzwMinimumCodeSize = data[counter++]; 
                lzwData = new byte[w * h]; 
                lzwDataIndex = 0;
                step = getLzwData; 
                break; 
            case 0x3b: // trailer    
                       //        Console.WriteLine ("trailer found!");    
                return false;
            default: 
                throw new Exception("Unknown main case: " + data[counter - 1]);
                //throw new Exception("something strange happened, default case: " + data[counter-1]);
                //return true;
                
        } 
 
        return true; 
 
    } 
 
    public string serialize() 
    { 
        string o = ""; 
         
        o += (char)'|'; 
         
        o += LCDwidth; 
         
        o += (char)'|'; 
         
        o += LCDheight; 
         
        o += (char)'|'; 
         
        o += this.delays.Count; 
         
        o += (char)'|'; 
         
        o += this.frames.Count; 
         
        o += (char)'|'; 
         
        foreach(var delay in this.delays) 
        { 
            o += delay; 
            o += (char)'|'; 
        } 
         
        foreach(var frm in this.frames) 
        { 
            o += new string(frm); 
            o += (char)'|'; 
        } 
         
        return o; 
    } 
     
    void unserialize(string s) 
    { 
        var parts = s.Split((char)'|'); 
         
        this.LCDwidth = Int32.Parse(parts[1]); 
        this.LCDheight = Int32.Parse(parts[2]); 
        var delays = Int32.Parse(parts[3]); 
        var frames = Int32.Parse(parts[4]); 
         
        for(var n = 0; n < delays; n++) 
            this.delays.Add(Int32.Parse(parts[5+n])); 
         
        for(var o = 0; o < frames; o++) 
            this.frames.Add(parts[5+delays+o].ToCharArray()); 
         
        return; 
    } 
     
    public Gif(int fwidth, int fheight, string base64) 
    { 
        if(base64[0] == '|') 
        { 
            this.unserialize(base64); 
            this.step = delegate() {return false;}; 
            return; 
        } 
         
        this.LCDwidth = fwidth; 
        this.LCDheight = fheight; 
         
        data = Convert.FromBase64String(base64); 
 
        string signature = "" + ((char)data[counter++]) + ((char)data[counter++]) + ((char)data[counter++]); 
        string version = "" + (char)data[counter++] + (char)data[counter++] + (char)data[counter++]; 
 
        if (signature != "GIF" || (version != "87a" && version != "89a")) 
        { 
            throw new Exception("Invalid gif file!"); 
        } 
 
        width = data[counter++] | (data[counter++] << 8); 
        height = data[counter++] | (data[counter++] << 8); 
 
        globalColorTableSize = (int)Math.Pow(2, (((data[counter] & 0x07)) + 1)); 
        bool globalColorTableSortFlag = (data[counter] & 0x08) > 0; 
        int colorResolution = ((data[counter] & 0x70) >> 4) + 1; 
        bool globalColorTableFlag = (data[counter] & 0x80) > 0; 
        counter++; 
 
        backgroundColor = data[counter++]; 
        byte aspectRatio = data[counter++]; 
 
        globalColorTable = new byte[globalColorTableSize][]; 
        floydSteinberg = new int[LCDwidth + 1,3]; // initialize dither array

        for (int i = 0; i < globalColorTableSize; i++) 
        { 
            globalColorTable[i] = new byte[3]; 
        } 
 
        if (!globalColorTableFlag) 
        { 
            globalColorTable[0][0] = 0x00; 
            globalColorTable[0][1] = 0x00; 
            globalColorTable[0][2] = 0x00; 
 
            globalColorTable[1][0] = 0xFF; 
            globalColorTable[1][1] = 0xFF; 
            globalColorTable[1][2] = 0xFF; 
        } 
        else  
        {
            for (int i = 0; i < globalColorTableSize; i++) 
            { 
                globalColorTable[i][0] = data[counter++]; 
                globalColorTable[i][1] = data[counter++]; 
                globalColorTable[i][2] = data[counter++]; 
            } 
        } 
        step = mainLoop; 
    } 
} 
 
public int frame = 0; 
public Gif gif; 
public List<IMyTextPanel> screens = new List<IMyTextPanel>();
public bool running = true; 
public long lastFrame  = 0; 
public int delay = 0;
