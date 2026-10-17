"""Render cursor-free, explicitly labelled SVG documentation illustrations."""
from pathlib import Path
from html import escape
import re

ROOT = Path(__file__).resolve().parent.parent
DEST = ROOT / 'docs/images/extension'
DEST.mkdir(parents=True, exist_ok=True)
MARK = re.findall(r'<path\b[^>]*/>', (ROOT/'assets/brand-mark.svg').read_text(encoding='utf-8'))
def text(x,y,value,size=22,fill='#272329',weight=400,anchor='start'):
    return f'<text x="{x}" y="{y}" font-size="{size}" fill="{fill}" font-weight="{weight}" text-anchor="{anchor}">{escape(value)}</text>'
def rect(x,y,w,h,fill='#FFFFFF',stroke='#DFD7DC',radius=10):
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{radius}" fill="{fill}" stroke="{stroke}"/>'
def badge(x,y,n):
    return f'<circle cx="{x}" cy="{y}" r="17" fill="#BA263A"/>'+text(x,y+6,str(n),19,'#FFFFFF',600,'middle')
def icon(x,y,size=46):
    return f'<svg x="{x}" y="{y}" width="{size}" height="{size}" viewBox="0 0 64 64">'+''.join(MARK)+'</svg>'
def save(path,title,description,body,width=1200,height=760):
    svg=f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}" role="img" aria-labelledby="title desc"><title id="title">{escape(title)}</title><desc id="desc">{escape(description)}</desc><g font-family="Segoe UI,Arial,sans-serif">{body}</g></svg>\n'
    path.write_text(svg,encoding='utf-8')
def frame(title,url):
    return rect(0,0,1200,760,'#F8F7F8','#DFD7DC',0)+text(40,52,title,30,weight=600)+text(40,86,'Setup illustration · browser layout may vary',18,'#655963')+rect(40,118,1120,58,'#FFFFFF')+text(64,155,url,25)

for browser,url,name,edge in [('Google Chrome','chrome://extensions','03-chromium-setup.svg',False),('Microsoft Edge','edge://extensions','04-edge-setup.svg',True)]:
    body=frame(f'Install Vydrop in {browser}',url)
    if edge:
        body+=rect(40,198,240,280,'#F1EDF0')+text(62,239,'Extensions',24,weight=600)+text(62,289,'My extensions',21)+badge(70,371,1)+text(96,378,'Developer mode',18)+rect(214,357,46,26,'#BA263A','#BA263A',13)+'<circle cx="246" cy="370" r="9" fill="#FFFFFF"/>'
        left=306
    else:
        body+=text(60,225,'Extensions',25,weight=600)+badge(874,218,1)+text(899,225,'Developer mode',18)+rect(1090,204,46,26,'#BA263A','#BA263A',13)+'<circle cx="1122" cy="217" r="9" fill="#FFFFFF"/>'
        left=60
    body+=badge(left+17,280,2)+rect(left+46,254,205,53,'#FFFFFF','#8E7D87')+text(left+67,288,'Load unpacked',22,weight=600)
    body+=rect(left,337,1120-(left-40)-40,126,'#FFFFFF')+icon(left+18,360)+text(left+78,382,'Vydrop',25,weight=600)+text(left+78,418,'Enabled · Details · Reload',19,'#655963')
    body+=badge(64,526,3)+text(92,533,'Select this extracted folder:',23,weight=600)+rect(62,553,1076,57,'#F8E3E9')+text(84,590,'Documents / Vydrop / Browser / chromium',25)
    body+=badge(64,657,4)+text(92,664,'Details → Site access → On all sites; refresh your video page.',22)+text(62,715,'Keep the folder in place. Chrome and Edge use the same chromium companion.',20,'#655963')
    save(DEST/name,f'{browser} extension setup',f'Illustration: open {url}, enable Developer mode, choose Load unpacked, select Browser/chromium, allow the desired sites and refresh the video page.',body)

body=frame('Install Vydrop temporarily in Firefox','about:debugging#/runtime/this-firefox')
body+=rect(40,198,230,290,'#F1EDF0')+text(62,240,'Setup',22)+rect(54,269,202,55,'#F8E3E9')+badge(78,296,1)+text(104,303,'This Firefox',21,weight=600)
body+=text(304,237,'Temporary Extensions',27,weight=600)+badge(321,291,2)+rect(350,264,330,54,'#FFFFFF','#8E7D87')+text(370,299,'Load Temporary Add-on…',22,weight=600)
body+=rect(304,346,832,115,'#FFFFFF')+icon(324,369)+text(383,393,'Vydrop',25,weight=600)+text(383,429,'Temporary extension · Reload · Remove',20,'#655963')
body+=badge(64,526,3)+text(92,533,'Select this file from your extracted app folder:',23,weight=600)+rect(62,553,1076,57,'#F8E3E9')+text(84,590,'Documents / Vydrop / Browser / firefox / manifest.json',24)
body+=badge(64,657,4)+text(92,664,'Refresh the page, play the video, then use Download by Vydrop.',22)+text(62,715,'Temporary installation ends when Firefox restarts. Load it again next session.',21,'#A12232',600)
save(DEST/'05-firefox-setup.svg','Firefox temporary extension setup','Illustration of This Firefox, Load Temporary Add-on and Browser/firefox/manifest.json. This unsigned local companion must be loaded again after a Firefox restart.',body)

body=rect(0,0,1200,760,'#F8F7F8',radius=0)+text(40,52,'Send the video you are watching to Vydrop',30,weight=600)+text(40,86,'Playback illustration · the tab stays outside the video picture',18,'#655963')
body+=rect(40,130,1120,540,'#FFFFFF')+rect(84,252,1032,330,'#20242A','#20242A')+text(600,425,'Your playing video',34,'#F6F2F4',500,'middle')
body+=rect(814,186,302,52,'#F1EDF0','#8E7D87')+icon(825,194,34)+text(868,219,'Download by Vydrop',20,weight=600)+badge(782,212,1)
body+=text(84,627,'Click the tab, then review the detected link, format and save folder in the app.',22)+text(62,719,'No tab visible? Open the Vydrop toolbar popup. Click Download in the app to start saving.',21,'#655963')
save(DEST/'04-send-video.svg','Send a playing video to Vydrop','Playback diagram showing the V-only icon beside Download by Vydrop above the right video border, with review in the desktop app before download.',body)

for theme,page,ink,field,border,accent,selected,muted in [('dark','#181B20','#F6F2F4','#282B32','#94828D','#CB2D42','#3E2933','#BEB2BB'),('light','#F8F7F8','#272329','#FFFFFF','#8E7D87','#BA263A','#F8E3E9','#655963')]:
    lockup=(ROOT/f'assets/brand-logo-{theme}.svg').read_text(encoding='utf-8')
    inner=re.search(r'<g transform=.*?</g>',lockup).group()
    body=rect(0,0,1000,1040,page,border,0)+rect(0,0,1000,32,'#20242A','#20242A',0)+icon(10,5,22)+text(40,23,'Vydrop',16,'#F6F2F4')
    body+=f'<svg x="34" y="52" width="170" height="56" viewBox="0 0 128 42">{inner}</svg>'+text(600,85,'Recent downloads',18,ink)+text(826,85,'English',18,ink)+f'<circle cx="943" cy="78" r="14" stroke="{ink}" fill="none" stroke-width="2"/><path d="M943 64A14 14 0 0 1 943 92Z" fill="{ink}"/>'
    body+='<path d="M0 132H1000" stroke="'+border+'"/>'+text(110,199,'New download',32,ink,600)+text(110,263,'Video link',20,ink,600)
    body+=rect(110,282,650,56,field,border)+text(130,318,'Videos, Shorts, and playlists',20,muted)+rect(776,282,114,56,field,border)+text(833,318,'Paste',20,ink,500,'middle')
    body+=text(110,398,'Save as',20,ink,600)+rect(110,420,380,60,selected,border)+text(300,459,'Video · MP4',22,ink,500,'middle')+rect(510,420,380,60,field,border)+text(700,459,'Audio · MP3',22,ink,500,'middle')
    body+=text(110,540,'Quality',20,ink,600)+rect(110,562,590,56,field,border)+text(130,598,'Best available',20,ink)+rect(720,562,170,56,field,border)+text(805,598,'Subtitles',20,ink,500,'middle')
    body+=text(110,682,'Save to',20,ink,600)+rect(110,704,610,56,field,border)+text(134,740,'Downloads',20,ink)+rect(736,704,154,56,field,border)+text(813,740,'Change',20,ink,500,'middle')
    body+=text(110,821,'+ Add to queue',19,muted)+text(890,821,'Queue (0)',19,ink,400,'end')+rect(330,858,340,62,accent,accent)+text(500,898,'Download',24,'#FFFFFF',600,'middle')
    body+='<path d="M0 975H1000" stroke="'+border+'"/>'+text(34,1011,'Settings',17,ink)+text(966,1011,'Contact',17,ink,400,'end')+text(500,1070,'UI illustration · Vydrop 4.2.0 · not a screenshot',18,'#655963',400,'middle')
    save(ROOT/f'docs/images/app-overview-{theme}.svg',f'Vydrop {theme} interface overview','Labelled illustration of the Vydrop 4.2.0 interface, red V wordmark with a solid white arrow, video/audio choice, subtitles, output folder and centered Download action. Not a native app screenshot.',body,1000,1090)

print('Rendered Chrome, Edge, Firefox and playback guides, plus both labelled app overviews.')
