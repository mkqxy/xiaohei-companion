using System;
using System.IO;
using System.Text;
namespace XiaoheiDesktop {
public static class MediaExport {
    // WIC's GIF encoder emits zero frame delays on some Windows builds even when
    // BitmapMetadata contains the delay. Correct only container timing, not pixels.
    public static void SetGifTiming(string path,ushort centiseconds){
        byte[] bytes=File.ReadAllBytes(path);if(bytes.Length<14||Encoding.ASCII.GetString(bytes,0,3)!="GIF")throw new InvalidDataException("Invalid exported GIF.");
        int prefix=13+((bytes[10]&128)!=0?3*(1<<((bytes[10]&7)+1)):0),position=prefix,frames=0,controls=0;bool loop=false,trailer=false;
        while(position<bytes.Length){
            int marker=bytes[position++];if(marker==0x3B){trailer=true;break;}
            if(marker==0x21){
                if(position>=bytes.Length)throw new InvalidDataException();int label=bytes[position++];
                if(label==0xF9){if(position+5>=bytes.Length||bytes[position]!=4)throw new InvalidDataException();bytes[position+2]=(byte)(centiseconds&255);bytes[position+3]=(byte)(centiseconds>>8);controls++;}
                if(label==0xFF&&position+12<=bytes.Length&&bytes[position]==11&&Encoding.ASCII.GetString(bytes,position+1,11)=="NETSCAPE2.0")loop=true;
                position=SkipSubBlocks(bytes,position);
            }else if(marker==0x2C){
                if(position+9>bytes.Length)throw new InvalidDataException();int packed=bytes[position+8];position+=9;if((packed&128)!=0)position+=3*(1<<((packed&7)+1));position++;position=SkipSubBlocks(bytes,position);frames++;
            }else throw new InvalidDataException("Unexpected GIF block.");
        }
        if(!trailer||frames==0||controls!=frames)throw new InvalidDataException("Exported GIF is incomplete.");
        if(loop){File.WriteAllBytes(path,bytes);return;}
        byte[] repeat={0x21,0xFF,0x0B,0x4E,0x45,0x54,0x53,0x43,0x41,0x50,0x45,0x32,0x2E,0x30,0x03,0x01,0x00,0x00,0x00};
        using(var output=File.Create(path)){output.Write(bytes,0,prefix);output.Write(repeat,0,repeat.Length);output.Write(bytes,prefix,bytes.Length-prefix);}
    }
    static int SkipSubBlocks(byte[] bytes,int position){while(true){if(position>=bytes.Length)throw new InvalidDataException();int count=bytes[position++];if(count==0)return position;position+=count;if(position>bytes.Length)throw new InvalidDataException();}}
}
}
