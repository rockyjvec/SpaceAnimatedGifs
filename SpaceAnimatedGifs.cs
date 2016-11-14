/************************************************************************************************************
    SpaceAnimagedGifs - A vanilla in-game script for playing animated gifs on LCDs
    
    How to load an animated gif into the game.
    
    1.  Go to http://spaceengineers.io/tools/storage-loader
    2.  Drag your animated gif file into the gray box on that page.
    3.  Depending on the size of the gif, it could take a little while to load.
    4.  Copy each script under the file name on that page into the programming block in game and run the PB
        once for each one.  (NOTE: Make sure you don't have a timer running the PB while your are doing this.
        If the script runs more than once for any of the storage loader scripts, it won't work)
    5.  Load this script into the PB and start a timer running the PB and then triggering itself.
    6.  Wait.  After a little while (can take up to a few minutes depending on how big the gif is) the gif will
        start playing.
        
    Note: Adjust the throttle below if you get complexity errors.
*************************************************************************************************************/

int throttle = 5000; // Set this lower to prevent complexity errors.  Set it higher to decrease loading times.

public class Decoder
{
    int MaxStackSize = 4096;
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
    Decoder decoder;

    // lSD   
    public int width;
    public int height;

    public int LCDwidth = 175;
    public int LCDheight = 175;

    int globalColorTableSize;
    byte[][] globalColorTable;
    int localColorTableSize;
    byte[][] localColorTable;
    byte[] data;
    long counter = 0;
    byte backgroundColor;
    bool gce = false;
    int lzwMinimumCodeSize;
    byte[] lzwData;
    int lzwDataIndex = 0;
    public Func<bool> step;
    long decodeBit = 0;
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

        if (x > LCDwidth - 1)
        {
            frame[x + ((LCDwidth + 1) * y)] = "\n"[0];
            x = 0;
            y++;
            if (y > (LCDheight - 1))
            {
                frames.Add(frame);

                step = mainLoop;
                y = 0;
                return true;
            }
        }
        
        bool draw = false;
        bool transparentPixel = false;
        if (sx >= left && sx < left + w && sy >= top && sy < top + h)
        {
            int spot = ((sy - top) * (w)) + (sx - left);
            if (spot < output.Length)
            {
                draw = true;
                byte index = output[spot];
                //	if(index < localColorTable.Length)
                color = localColorTable[index];
                if (!this.is_transparent && index == transparent) 
                {
                    transparentPixel = true;
                    draw = false;
                }
            }
        }

        if(this.restore_background  && this.last_do_not_dispose && sx < width && sy < height && !draw && !transparentPixel)
        {
            draw = true;
        }
        
        if (draw)
            frame[x + ((LCDwidth + 1) * y)] = (char)('\uE100' + (color[2] * 8 / 256) + ((color[1] * 8 / 256) * 8) + ((color[0] * 8 / 256) * 64));

        x += 1;

        return true;
    }

    bool decode()
    {
        if (this.decoder.decode())
        {
            return true;
        }
        else
        {
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
        step = decode;
        return true;
    }

    bool getLzwData()
    {
        int len = data[counter++];
        for (int i = 0; i < len; i++)
        {
            lzwData[lzwDataIndex++] = data[counter++];
        }

        if (data[counter] == 00)
        {
            counter++;
            step = decodeStart;
        }
        return true;
    }

    bool extensionLoop()
    {
        counter += data[counter++];
        if (data[counter++] == 0x00)
            step = mainLoop;
        return true;
    }

    bool mainLoop()
    {
        if (counter > data.Length)
            return false;

        gce = false; // had graphics control extension   
        switch (data[counter++])
        {
            case 0x21: // extension   
                switch (data[counter++])
                {
                    case 0xF9: // graphic control extension   
                        gce = true;
                        counter++; // Block size 0x04
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
                        counter++; // Block Terminator (0x00)
                        break;
                    default:
                        step = extensionLoop;
                        return true;
                }
                break;
            case 0x2c: // image descriptor   
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
                else {
                    localColorTableSize = globalColorTableSize;
                    localColorTable = globalColorTable;
                }

                lzwMinimumCodeSize = data[counter++];
                lzwData = new byte[w * h];
                lzwDataIndex = 0;
                step = getLzwData;
                break;
            case 0x3b: // trailer   
                       //		Console.WriteLine ("trailer found!");   
                return false;
        }

        return true;

    }

    public Gif(string base64)
    {
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
public IMyTextPanel screen;
public bool running = true;
public long lastFrame  = 0;

void Main (string args)
{
    if(gif == null)
    {
        gif = new Gif(Storage);        
        screen = GridTerminalSystem.GetBlockWithName("SpaceGameboy LCD") as IMyTextPanel;
        return;
    }
    
    if(running)
    {
        int count = 0;
        while (count++ < throttle)
        {
            if(!gif.step())
            {
                running = false;
                break;
            }
        }
        return;
    }
    
    long now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
    if(now - lastFrame > gif.delays[frame % gif.frames.Count] * 10)
    {
        screen.WritePublicText(new String(gif.frames[frame % gif.frames.Count]), false);
        lastFrame = now;
        frame++;
    }
}
