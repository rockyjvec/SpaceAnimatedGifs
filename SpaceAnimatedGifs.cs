public 	class Gif  
	{  
		// lSD  
		public int width;  
		public int height;  
  
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
  
		char[] frame;  
		public List<char[]> frames = new List<char[]>();  
		public List<int> delays;  
		const char green = '\uE001';                 
		const char blue  = '\uE002';                 
		const char red   = '\uE003';                 
		const char black = '\uE00D';    
		int getCode()  
		{  
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
  
		bool createFrame()  
		{  
			byte[] color = new byte[3] {0, 0, 0} ;
int spot = ((y/3) * (width + 1)) + (x/3);
if(spot < output.Length)
{
		color = localColorTable[output [spot]];  
}
  
  
			if (x > (width * 3) - 3) {				  
frame[x + 1 + ((width*3) * y)] = "\n"[0]; 
				x = 0;  
				y = y + 3;  
				if (y >= ((height*3) - 3 )) {  
					frames.Add (frame);  
 
					step = mainLoop;  
					y = 0;  
					return true;  
				}  
  
				int r = color [0] * 8 / 256;  
				int g = color [1] * 8 / 256;  
				int b = color [2] * 8 / 256;  
  
				if ((r & 1) > 0)  
					frame [(y * ((width*3) )) + x] = red;  
				else   
					frame [(y * ((width*3) )) + x] = black;  
				if ((g & 1) > 0)  
					frame [(y * ((width*3) )) + x + 1] = green;  
				else   
					frame [(y * ((width*3) )) + x + 1] = black;  
				if ((b & 1) > 0)  
					frame [(y * ((width*3) )) + x + 2] = blue;  
				else   
					frame [(y * ((width*3) )) + x + 2] = black;  
  
				if ((r & 2) > 0)  
					frame [((y+1) * ((width*3) )) + x] = red;  
				else   
					frame [((y+1) * ((width*3) )) + x] = black;  
				if ((g & 2) > 0)  
					frame [((y+1) * ((width*3) )) + x + 1] = green;  
				else   
					frame [((y+1) * ((width*3) )) + x + 1] = black;  
				if ((b & 2) > 0)  
					frame [((y+1) * ((width*3) )) + x + 2] = blue;  
				else   
					frame [((y+1) * ((width*3) )) + x + 2] = black;  
  
				if ((r & 4) > 0)  
					frame [((y+2) * ((width*3) )) + x] = red;  
				else   
					frame [((y+2) * ((width*3) )) + x] = black;  
				if ((g & 4) > 0)  
					frame [((y+2) * ((width*3) )) + x + 1] = green;  
				else   
					frame [((y+2) * ((width*3) )) + x + 1] = black;  
				if ((b & 4) > 0)  
					frame [((y+2) * ((width*3) )) + x + 2] = blue;  
				else   
					frame [((y+2) * ((width*3) )) + x + 2] = black;  
  
  
			}  
			x+=3;  
  
			return true;  
		}  
  
		bool decode()  
		{  
 
		//	while (true) {  
				if (Math.Pow (2, lzwCodeSize) == idx) {  
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
 
					frame = new char[((width * 3) + 1) * (height * 3)];  
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
					dictionary.Add (idx++, dictionary [lastcode] + (char)K);  
					} else {  
					K = (byte)dictionary [lastcode][0];  
					output += dictionary [lastcode] + (char)K;  
		//				Console.WriteLine ("add b#" + idx + " " + (((byte)dictionary [lastcode][0])) + " " + ((byte)K));  
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
					counter += data[counter++] + 1; // temporarily skip it until it is implemented  
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
			if (base64.Substring (0, 22) == "data:image/gif;base64,")  
				data = Convert.FromBase64String (base64.Substring (22));  
			else  
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
		 void Main (string args)  
		{  
if(aaas == null) 
{ 
		 aaas = new Gif("data:image/gif;base64,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");  
            screen = GridTerminalSystem.GetBlockWithName("SpaceGameboy LCD") as IMyTextPanel;   
  
} 
if(running) 
{ 
int count = 0; 
			while (count++ < 1000) {    
//			Console.WriteLine (a.frames.Count);    
if(!aaas.step())
{
running = false;
break;
}
		}    
 return;
} 
//throw new Exception("done");      
 
        screen.ShowTextureOnScreen();  
        screen.ShowPublicTextOnScreen();  
          
        screen.WritePublicText(new String(aaas.frames[frame % aaas.frames.Count]), false);      
             
        screen.ShowTextureOnScreen();   
        screen.ShowPublicTextOnScreen();   
 frame++; 
 
} 
 
