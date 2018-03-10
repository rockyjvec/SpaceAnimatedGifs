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

bool cache = true; // permanently cache the frames from the GIF so it doesn't need to be decoded every time.

int frameSkip = 0; // skips every frameSkip frames to increase performance.

int throttle = 7000; // Set this lower to prevent complexity errors.  Set it higher to decrease loading times. 
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
            }
        }
        else
        {
            throw new Exception("LCD named \"" + lcdName + "\" not found.");
        }
    }
    gif = new Gif(frameWidth, frameHeight, Storage);
    Gif.Ech = Echo;
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
                    frames.Add(Int32.Parse(number));
                }
            }
        }        
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
    int MaxStackSize = 8192; 
    int width = 0; 
    int height = 0; 
 
    int dataSize = 0; 
 
    int NullCode = -1; 
    int pixelCount = 0; 
    public byte[] pixels; 
    int codeSize; 
    int clearFlag; 
    int endFlag; 
    int available; 
 
    int code; 
    int old_code; 
    int code_mask; 
    int bits; 
 
    int[] prefix; 
    int[] suffix; 
    int[] pixelStack; 
 
    int top; 
    int count; 
    int bi; 
    int i; 
 
    int data; 
    int first; 
    int inCode; 
 
    byte[] buffer; 
 
    public Decoder(int width, int height, byte[] buffer, int minCodeSize) 
    { 
        this.buffer = buffer; 
        this.width = width; 
        this.height = height; 
        this.dataSize = minCodeSize; 
 
        this.NullCode = -1; 
        this.pixelCount = width * height; 
        this.pixels = new byte[pixelCount]; 
        this.codeSize = dataSize + 1; 
        this.clearFlag = 1 << dataSize; 
        this.endFlag = clearFlag + 1; 
        this.available = endFlag + 1; 
 
        this.code = NullCode; 
        this.old_code = NullCode; 
        this.code_mask = (1 << codeSize) - 1; 
        this.bits = 0; 
 
        this.prefix = new int[MaxStackSize]; 
        this.suffix = new int[MaxStackSize]; 
        this.pixelStack = new int[MaxStackSize + 1]; 
 
        this.top = 0; 
        this.count = buffer.Length; 
        this.bi = 0; 
        this.i = 0; 
 
        this.data = 0; 
        this.first = 0; 
        this.inCode = NullCode; 
 
        for (code = 0; code < clearFlag; code++) 
        { 
            prefix[code] = 0; 
            suffix[code] = (byte)code; 
        } 
    } 
 
    public bool decode() 
    { 
        if (i < pixelCount) 
        { 
            if (top == 0) 
            { 
                if (bits < codeSize) 
                { 
/*                    if (count == 0) 
                    { 
                        //    buffer = ReadData(); 
                        //    count = buffer.Length;                           
                        if (count == 0) 
                        { 
                            //throw new Exception("got here"); 
                            return false; 
                        } 
                        bi = 0; 
                    }*/ 
                    data += buffer[bi] << bits; 
                    bits += 8; 
                    bi++; 
                    count--; 
                    return true; 
                } 
                code = data & code_mask; 
                data >>= codeSize; 
                bits -= codeSize; 
 
 
                if (code > available || code == endFlag) 
                { 
                    return false; 
                } 
                if (code == clearFlag) 
                { 
                    codeSize = dataSize + 1; 
                    code_mask = (1 << codeSize) - 1; 
                    available = clearFlag + 2; 
                    old_code = NullCode; 
                    return true; 
                } 
                if (old_code == NullCode) 
                { 
                    pixelStack[top++] = suffix[code]; 
                    old_code = code; 
                    first = code; 
                    return true; 
                } 
                inCode = code; 
                if (code == available) 
                { 
                    pixelStack[top++] = (byte)first; 
                    code = old_code; 
                } 
                while (code > clearFlag) 
                { 
                    pixelStack[top++] = suffix[code]; 
                    code = prefix[code]; 
                } 
                first = suffix[code]; 
                if (available > MaxStackSize) 
                { 
                    return false; 
                } 
                pixelStack[top++] = suffix[code]; 
                prefix[available] = old_code; 
                suffix[available] = first; 
                available++; 
                if (available == code_mask + 1 && available < MaxStackSize) 
                { 
                    codeSize++; 
                    code_mask = (1 << codeSize) - 1; 
                } 
                old_code = inCode; 
            } 
            top--; 
            pixels[i++] = (byte)pixelStack[top]; 
            return true; 
        } 
        else 
        { 
            return false; 
        } 
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
 
    char[] frame, last; 
    public List<char[]> frames = new List<char[]>(); 
    public List<int> delays = new List<int>(); 
    public int totalFrames = 0;
    
    public static Action<string> Ech;
    public void Echo(string e)
    {
        if(debug) Ech(e);
    }
 
    bool createFrame() 
    { 
        byte[] color = localColorTable[backgroundColor]; 
         
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
        bool transparentPixel = false; 
        if (sx >= left && sx < left + w && sy >= top && sy < top + h) 
        { 
            int spot = ((sy - top) * (w)) + (sx - left); 
            if (spot < output.Length) 
            { 
                draw = true; 
                byte index = output[spot]; 
                //    if(index < localColorTable.Length) 
                color = localColorTable[index]; 
                if (!this.is_transparent && index == transparent)  
                { 
                    transparentPixel = true; 
                    draw = false; 
                } 
            } 
        } 
 
        if(this.restore_background && this.last_do_not_dispose && sx < width && sy < height && !draw && (!transparentPixel)) 
        { 
            draw = true; 
        } 
         
        if (draw)
        {
            frame[x + ((LCDwidth + 1) * y)] = (char)('\uE100' + Math.Floor((double)color[2] * 8.0 / 256.0) + (Math.Floor((double)color[1] * 8.0 / 256.0) * 8) + (Math.Floor((double)color[0] * 8.0 / 256.0) * 64)); 
        }
        
        if(frame[x + ((LCDwidth + 1) * y)] < '\uE100')
        {
            frame[x + ((LCDwidth + 1) * y)] = (char)('\uE100' + ((double)localColorTable[backgroundColor][2] * 8.0 / 256.0) + (((double)localColorTable[backgroundColor][1] * 8.0 / 256.0) * 8.0) + (((double)localColorTable[backgroundColor][0] * 8.0 / 256.0) * 64.0)); 
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
                Ech("Frame " + frames.Count + " added.");
                step = mainLoop; 
                y = 0; 
                return true; 
            } 
        } 
 
        return true; 
    } 
 
    bool decode() 
    { 
        if (maxSteps == 0 && this.decoder.decode()) 
        { 
            return true; 
        } 
        else 
        {
            if(maxSteps == 0)
            {
                maxSteps = 1;
                return true;
            }
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
    } 
 
    bool decodeStart() 
    { 
        this.decoder = new Decoder(w, h, lzwData, lzwMinimumCodeSize);
        maxSteps = 0;
        step = decode; 
        return true; 
    } 
 
    bool getLzwData() 
    {       
        maxSteps = 0;
        int len = data[counter++]; 
        for (int i = 0; i < len; i++) 
        { 
            lzwData[lzwDataIndex++] = data[counter++]; 
        } 
 
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
                        // 0 -   No disposal specified. The decoder is 
                        //       not required to take any action. 
                        // 1 -   Do not dispose. The graphic is to be left 
                        //       in place. 
                        this.last_do_not_dispose = this.do_not_dispose; 
                        this.do_not_dispose = (flags & 0x2) > 0; 
                         
                        // 2 -   Restore to background color. The area used by the 
                        //       graphic must be restored to the background color. 
                        this.restore_background = (flags & 0x4) > 0; 
                         
                        // 3 -   Restore to previous. The decoder is required to 
                        //       restore the area overwritten by the graphic with 
                        //       what was there prior to rendering the graphic. 
                        this.is_transparent = (flags & 0x8) > 0; 
 
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
