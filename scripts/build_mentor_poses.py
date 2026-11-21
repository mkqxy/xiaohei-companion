from pathlib import Path
import numpy as np
from PIL import Image
from scipy.spatial import Delaunay
from scipy.ndimage import map_coordinates

# Alignment coordinates measured on the two 490-pixel-tall character cels.
rest = [(75,7),(115,37),(92,52),(103,74),(85,94),(115,106),(54,91),
        (12,165),(2,224),(21,267),(22,308),(101,206),(148,182),
        (158,162),(208,154),(184,173),(146,264),(26,198),(105,198),
        (18,449),(125,446),(36,490),(109,487)]
catch = [(96,7),(135,37),(114,54),(117,78),(96,98),(119,112),(59,88),
         (13,158),(0,212),(19,254),(23,306),(102,202),(134,190),
         (148,183),(194,175),(176,193),(136,264),(26,192),(99,193),
         (11,448),(128,446),(35,491),(110,487)]
scale=1.8
size=(int(218*scale),int(500*scale))
corners=[(0,0),(217,0),(217,499),(0,499)]
pairs=[np.array(points+corners)*scale for points in (rest,catch)]
base=Path(__file__).resolve().parents[1]
project=base/'assets/wuxian'
art=[]
for name in ['full','catch']:
    image=Image.open(project/(name+'.png')).convert('RGBA')
    image.thumbnail((int(340*scale),int(490*scale)),Image.Resampling.LANCZOS)
    canvas=Image.new('RGBA',size)
    canvas.paste(image,(0,round(5*scale)))
    rgba=np.asarray(canvas).astype(np.float32)/255
    rgba[:,:,:3]*=rgba[:,:,3:]
    art.append(rgba)
y,x=np.indices(size[::-1])
grid=np.stack((x,y),-1)
strip=Image.new('RGBA',(size[0]*6,size[1]))
for frame in range(6):
    t=frame/5
    mid=pairs[0]*(1-t)+pairs[1]*t
    tri=Delaunay(mid)
    index=tri.find_simplex(grid)
    mat=tri.transform[index]
    bary=np.einsum('...ij,...j->...i',mat[:,:,:2],grid-mat[:,:,2])
    weights=np.dstack((bary,1-bary.sum(axis=2)))
    mixed=np.zeros_like(art[0])
    for points,pixels,weight in zip(pairs,art,[1-t,t]):
        uv=np.einsum('...i,...ij->...j',weights,points[tri.simplices[index]])
        for chan in range(4):
            mixed[:,:,chan]+=map_coordinates(pixels[:,:,chan],(uv[:,:,1],uv[:,:,0]),order=1)*weight
    mixed[index<0]=0
    mixed[:,:,:3]/=np.maximum(mixed[:,:,3:],.00001)
    strip.paste(Image.fromarray(np.clip(mixed*255+.5,0,255).astype('uint8')),(frame*size[0],0))
strip.save(project/'mentor.png',optimize=True)
