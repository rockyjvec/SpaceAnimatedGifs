public 	class Gif   
	{   
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
		Dictionary<int, string> dictionary;   
		long counter = 0;   
		byte backgroundColor;   
		bool gce = false;   
		int lzwMinimumCodeSize;   
		int lzwCodeSize;   
		string lzwData = "";   
		public Func<bool> step;   
		long decodeBit = 0;   
		long clearCode, endCode;   
		string output = "";   
		int idx = 0;   
		int lastcode,code;   
		int top, left, w, h;   
		bool interlaceFlag;   
		int x, y;
        int transparent = 0;
        bool is_transparent = false;
        bool restore_background = false;
        bool do_not_dispose = false;
   
		char[] frame, last;   
		public List<char[]> frames = new List<char[]>();   
		public List<int> delays = new List<int>(); 
   
		bool createFrame()   
		{   
			byte[] color = globalColorTable[backgroundColor];
            float scale = 1;
            if(width > height)
            {
                scale = (float)((float)width / (float)LCDwidth);
            }
            else
            {
                scale = (float)((float)height / (float)LCDheight);
            }
            int sx = (int)(x * scale);
            int sy = (int)(y * scale);            
            
			if (x > LCDwidth -1) {				   
                frame[x + ((LCDwidth+1) * y)] = "\n"[0];  
				x = 0;   
				y++;   
				if (y > (LCDheight - 1)) {   
					frames.Add (frame);   
  
					step = mainLoop;   
					y = 0;   
					return true;   
				}   
			}
            bool draw = true;
            if(sx >= left && sx < left + w && sy >= top && sy < top + h)
            {
                int spot = ((sy - top) * (w)) + (sx-left); 
                if(spot < output.Length) 
                { 
                    color = localColorTable[output [spot]];
                    if(output[spot] == transparent) draw = false;
                }                 
            }

            if(draw)
                frame[x +((LCDwidth +1) * y)] = (char)('\uE100'+(color[2]*8/256)+((color[1]*8/256)*8)+((color[0]*8/256)*64)); 
   
   
			x+=1;   
   
			return true;   
		}   
            uint mask = 0x01;

		int getCode()   
		{   
            /*var code = 0x0;
            // Always read one more bit than the code length
            for (var i = 0; i < ( lzwCodeSize + 1 ); i++ )
            {
              // This is different than in the file read example; that 
              // was a call to "next_bit"
              var bit = (( lzwData[(int)(decodeBit + i ) / (int)8] & mask )>0) ? 1 : 0;
              mask  <<= 1;

              if ( mask == 0x100 )
              {
                mask = 0x01;
              }

              code = code | ( bit << i );
            }
            return code;
*/
        int val = 0;   
			for (int i = 0; i < lzwCodeSize; i++) {   
				if (((int)(decodeBit + i) / (int)8) >= lzwData.Length)   
					continue;   
				byte b = (byte)(lzwData[(int)(decodeBit + i) / (int)8]);   
//				Console.WriteLine("" + (decodeBit + i) + " / " + 8 + " = " + ((int)((decodeBit + i) / (int)8)) + "(" + (decodeBit + i) + ") " +((decodeBit + i) % 8));   
				if((b & (int)(Math.Pow(2, (decodeBit + i) % 8))) > 0)   
				{   
					val = val | (int)(Math.Pow(2, (i)));   
//					Console.WriteLine ("*" + val);   
   
				}   
			}   
//			Console.WriteLine ("returning(" + lzwCodeSize + ") " + val);   
			decodeBit += lzwCodeSize;   
	//		if (decodeBit > 32)   
	//			throw new Exception ("stop");   
			return val;   
		}   
   
		bool decode()   
		{   
  
		//	while (true) {   
				if (Math.Pow (2, lzwCodeSize) == idx && lzwCodeSize < 12) {   
					lzwCodeSize++;   
	//				Console.WriteLine ("upping stuff");   
				}   
				code = getCode ();   
				//Console.WriteLine("" + code);   
				if (code == clearCode) {   
					dictionary.Clear ();   
					for (idx = 0; idx < Math.Pow (2, lzwMinimumCodeSize); idx++) {   
						dictionary.Add (idx, "" + (char)idx);   
					}   
					idx++;   
					idx++;   
					lzwCodeSize = lzwMinimumCodeSize + 1;   
					code = getCode ();   
		//			Console.WriteLine ("Clearing...");   
				}   
				if (code == endCode) {   
					x = 0;   
					y = 0;   
  
					
                    if(last == null) last = new char[(LCDwidth +1) * LCDheight];
                    if(frame != null) last = frame;
                    frame = new char[(LCDwidth + 1) * LCDheight];   
                    Array.Copy(last, frame, frame.Length);
                    
					step = createFrame;   
					return true;   
				}   
//					Console.WriteLine ("Start");   
					byte K;   
					if (dictionary.ContainsKey (code)) {   
						output += dictionary [code];   
						K = (byte) dictionary [code][0];   
   
		//			Console.Write ("add a#" + idx + " ");   
/*					for(int n = 0; n < dictionary[lastcode].Length;n++)   
					{   
						Console.Write (((byte)dictionary [lastcode][n]) + " ");   
					}   
						Console.Write (((byte)K) + " ");   
					Console.Write("\n");*/   
                    if(dictionary.ContainsKey(lastcode))
					dictionary.Add (idx++, dictionary [lastcode] + (char)K);   
					} else        if(dictionary.ContainsKey(lastcode)){   
					K = (byte)dictionary [lastcode][0];   
					output += dictionary [lastcode] + (char)K;   
		//				Console.WriteLine ("add b#" + idx + " " + (((byte)dictionary [lastcode][0])) + " " + ((byte)K));   
                    if(dictionary.ContainsKey(lastcode))
					dictionary.Add (idx++, dictionary [lastcode] + (char)K);   
//						Console.WriteLine ("didn't have it");   
					}   
			//		Console.WriteLine ("Dictionary: ");   
/*				foreach (var dic in dictionary) {   
					Console.Write ("#" + dic.Key+" ");   
					for (int n = 0; n < dic.Value.Length; n++) {   
						Console.Write ("," + (byte)dic.Value [n]);   
					}   
					Console.Write ("\n");   
				}   
				Console.WriteLine ("Index Stream: ");   
				for (int n = 0; n < output.Length; n++) {   
					Console.Write ("," + (byte)output[n]);   
				}   
				Console.Write ("\n");*/   
   
				lastcode = code;   
		//		}   
		//	}   
			return true;   
		}   
   
		bool decodeStart()   
		{   
			output = "";   
			dictionary = new Dictionary<int, string> ();   
			decodeBit = 0;   
			lzwCodeSize = lzwMinimumCodeSize + 1;   
			clearCode = (long)Math.Pow (2, lzwMinimumCodeSize);   
			endCode = clearCode + 1;   
			lastcode = 1;   
   
			code = getCode ();   
			if (code == clearCode) {   
				dictionary.Clear ();   
				for (idx = 0; idx < Math.Pow (2, lzwMinimumCodeSize); idx++) {   
					dictionary.Add (idx, "" + (char)idx);   
				}   
				idx++;   
				idx++;   
				code = getCode ();   
			}   
			output += (char)1;   
			/*			for (idx = 0; idx < Math.Pow(2, lzwMinimumCodeSize); idx++) {   
				Console.WriteLine ("adding idx " + idx);   
				dictionary.Add (idx, ""+(char)idx);   
			}   
			idx++;    
			idx++;   
			code = getCode ();   
			lastcode = 1;   
			Console.WriteLine ("first code("+Math.Pow(2, lzwMinimumCodeSize)+"): " + 1);*/   
			step = decode;   
			return true;   
		}   
   
		bool getLzwData()   
		{   
			int len = data [counter++];   
			for (int i = 0; i < len; i++) {   
				lzwData += (char)data [counter++];   
			}   
   
			if(data[counter] == 00)   
			{   
				counter++;   
				step = decodeStart;   
			}   
			return true;   
		}   
   
		bool extensionLoop() {   
			counter += data[counter++];   
			if (data [counter++] == 0x00)   
				step = mainLoop;   
			return true;   
		}   
   
		bool mainLoop() {   
			if (counter > data.Length)   
				return false;   
   
			gce = false; // had graphics control extension   
			switch(data[counter++])   
			{   
			case 0x21: // extension   
				switch(data[counter++])   
				{   
				case 0xF9: // graphic control extension   
					gce = true;   
                    counter++; // Block size 0x04
                    counter++; // Flags
                    this.delays.Add(data[counter++] | (data[counter++] << 8)); // Delay Time
                    transparent = data[counter++]; // Transparent Color Index
                    counter++; // Block Terminator (0x00)
					//counter += data[counter++] + 1; // temporarily skip it until it is implemented   
					break;   
				default:   
					step = extensionLoop;   
					return true;   
				}   
				break;   
			case 0x2c: // image descriptor   
  
				left = data [counter++] | (data [counter++] << 8);   
				top = data [counter++] | (data [counter++] << 8);   
   
				w = data [counter++] | (data [counter++] << 8);   
				h = data [counter++] | (data [counter++] << 8);   
   
				bool localColorTableFlag = (data [counter] & 0x80) > 0;   
				interlaceFlag = (data [counter] & 0x40) > 0;   
				bool sortFlag = (data [counter] & 0x20) > 0;   
				localColorTableSize = (int)Math.Pow (2, (((data [counter] & 0x07)) + 1));   
				counter++; // skip packed field used above   
   
				if (localColorTableFlag) {   
					localColorTable = new byte[localColorTableSize] [];   
					for (int i = 0; i < localColorTableSize; i++) {   
						localColorTable [i] = new byte[3];   
					}   
					for (int i = 0; i < localColorTableSize; i++) {   
						localColorTable [i] [0] = data [counter++];   
						localColorTable [i] [1] = data [counter++];   
						localColorTable [i] [2] = data [counter++];   
					}   
				} else {   
					localColorTableSize = globalColorTableSize;   
					localColorTable = globalColorTable;   
				}   
   
				lzwMinimumCodeSize = data [counter++];   
				lzwData = "";   
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
			data = Convert.FromBase64String (base64);   
   
			string signature = "" + ((char)data [counter++]) + ((char)data [counter++]) + ((char)data [counter++]);   
			string version = "" + (char)data [counter++] + (char)data [counter++] + (char)data [counter++];   
   
			if (signature != "GIF" || (version != "87a" && version != "89a")) {   
				throw new Exception ("Invalid gif file!");   
			}   
   
			width = data [counter++] | (data [counter++] << 8);   
			height = data [counter++] | (data [counter++] << 8);   
   
			globalColorTableSize = (int)Math.Pow(2, (((data [counter] & 0x07)) + 1));   
			bool globalColorTableSortFlag = (data [counter] & 0x08) > 0;   
			int colorResolution = ((data [counter] & 0x70) >> 4) + 1;   
			bool globalColorTableFlag = (data [counter] & 0x80) > 0;   
			counter++;   
   
			backgroundColor = data [counter++];   
			byte aspectRatio = data [counter++];   
   
			globalColorTable = new byte[globalColorTableSize] [];   
   
			for (int i = 0; i < globalColorTableSize; i++) {   
				globalColorTable [i] = new byte[3];   
			}   
   
			if (!globalColorTableFlag) {   
				globalColorTable [0] [0] = 0x00;   
				globalColorTable [0] [1] = 0x00;   
				globalColorTable [0] [2] = 0x00;   
   
				globalColorTable [1] [0] = 0xFF;   
				globalColorTable [1] [1] = 0xFF;   
				globalColorTable [1] [2] = 0xFF;   
			} else {   
				for (int i = 0; i < globalColorTableSize; i++) {   
					globalColorTable [i] [0] = data [counter++];   
					globalColorTable [i] [1] = data [counter++];   
					globalColorTable [i] [2] = data [counter++];   
				}   
			}   
			step = mainLoop;   
		}   
	}   
   
public int frame = 0; 
 public Gif aaas;  
public IMyTextPanel screen;  
public bool running = true;
public long lastFrame  = 0;
		 void Main (string args)   
		{   
if(aaas == null)  
{  
		 aaas = new Gif(Storage);   
            screen = GridTerminalSystem.GetBlockWithName("SpaceGameboy LCD") as IMyTextPanel;    
  }  
if(running)  
{  
int count = 0;  
			while (count++ < 1500) {     
//			Console.WriteLine (a.frames.Count);     
if(!aaas.step()) 
{ 
running = false; 
break; 
} 
		}     
 return; 
}
long now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
//throw new Exception("done");       
if(now - lastFrame > aaas.delays[frame % aaas.frames.Count] * 10)  
{
        screen.WritePublicText(new String(aaas.frames[frame % aaas.frames.Count]), false);                     
        lastFrame = now;
        frame++;  
}
        
  
}  
  
