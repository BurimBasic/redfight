import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {createRequire} from 'node:module';
const require=createRequire(import.meta.url);require('./ayumi.cjs');
const score=JSON.parse(fs.readFileSync(new URL('./score.json',import.meta.url),'utf8'));
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const clock=1773400,frameRate=50,sampleRate=44100,seconds=score.bars*4*60/score.bpm,frames=Math.ceil(seconds*frameRate),ticksPerSecond=score.bpm*48/60;
const data=new Uint8Array(frames*16),ay=new globalThis.Ayumi();ay.configure(false,clock,sampleRate);for(let ch=0;ch<3;ch++)ay.setPan(ch,.5,false);
function noteAt(track,tick,hold=false){const events=score.tracks[track].events;let lo=0,hi=events.length-1,found=-1;while(lo<=hi){let mid=(lo+hi)>>1;if(events[mid].pos<=tick){found=mid;lo=mid+1;}else hi=mid-1;}if(found<0)return null;let e=events[found];return hold||tick<e.pos+e.len?e:null;}
const frequency=n=>440*2**((n-69)/12),period=n=>Math.max(1,Math.min(4095,Math.round(clock/(16*frequency(n)))));
let lastBass=-1,lastBassNote=-1;const stats={toneChannels:3,clock,frameRate,frames,seconds:frames/frameRate,envelopeWrites:0,noiseFrames:0,ayMode:true};
for(let frame=0;frame<frames;frame++){
 const t=frame/frameRate,tick=t*ticksPerSecond,bar=Math.min(score.bars-1,Math.floor(tick/192)),bridge=bar>=36&&bar<44;let r=data.subarray(frame*16,frame*16+16);r[7]=0x3f;r[13]=255;
 function voice(ch,p,v,tone=true,noise=false,env=false){r[ch*2]=p&255;r[ch*2+1]=p>>8;r[8+ch]=env?16:v;if(tone)r[7]&=~(1<<ch);if(noise)r[7]&=~(8<<ch);}
 const lead=noteAt(0,tick);
 if(lead){const age=(tick-lead.pos)/ticksPerSecond,vib=age>.14?Math.sin(age*Math.PI*2*5.8)*.13:0;let v=bridge?11:14;if(age>.04)v--;if(age>.15)v--;if(age>.30)v--;voice(0,period(lead.midi+vib),Math.max(8,v));}
 const bass=noteAt(3,tick);
 if(bass){let p=period(bass.midi),env=Math.max(1,Math.round(clock/(256*frequency(bass.midi))));voice(1,p,0,false,false,true);r[11]=env&255;r[12]=env>>8;if(bass.pos!==lastBass||bass.midi!==lastBassNote){r[13]=8;stats.envelopeWrites++;lastBass=bass.pos;lastBassNote=bass.midi;}}
 const kick=noteAt(5,tick,true),snare=noteAt(6,tick,true),hat=noteAt(7,tick,true);
 const ka=kick?(tick-kick.pos)/ticksPerSecond:100,sa=snare?(tick-snare.pos)/ticksPerSecond:100,ha=hat?(tick-hat.pos)/ticksPerSecond:100;
 if(ka<.06){r[6]=22;let p=Math.round(clock/(16*(145-110*ka/.06)));voice(2,p,Math.max(5,14-Math.floor(ka*120)),true,true);stats.noiseFrames++;}
 else if(sa<.06){r[6]=9+Math.floor(sa*100);voice(2,1,Math.max(5,13-Math.floor(sa*110)),false,true);stats.noiseFrames++;}
 else if(ha<.025&&!bridge){r[6]=2;voice(2,1,7,false,true);stats.noiseFrames++;}
 else {
   const pattern=score.tracks[4].patterns[bar]??[];const chord=[...new Set(pattern.map(n=>n.midi))].sort((a,b)=>a-b);
   if(chord.length){let n=chord[Math.floor(frame/(bridge?2:1))%chord.length];voice(2,period(n),bridge?7:10);}
 }
 for(let ch=0;ch<3;ch++){const p=r[ch*2]|r[ch*2+1]<<8;if(p>4095||r[ch*2+1]>15||r[8+ch]>16)throw new Error('Invalid AY register');}
 if(r[6]>31)throw new Error('Invalid noise period');
}
const nSamples=frames*(sampleRate/frameRate),audio=new Float32Array(nSamples);let peak=0,sum=0;
console.log('Rendering AY-3-8910 at '+clock+' Hz, 3 channels / hardware envelope / shared noise');
for(let frame=0,at=0;frame<frames;frame++){
 const r=data.subarray(frame*16,frame*16+16);for(let ch=0;ch<3;ch++){ay.setTone(ch,r[ch*2]|r[ch*2+1]<<8);ay.setMixer(ch,r[7]>>ch&1,r[7]>>(ch+3)&1,r[ch+8]>>4&1);ay.setVolume(ch,r[ch+8]&15);}ay.setNoise(r[6]);ay.setEnvelope(r[11]|r[12]<<8);if(r[13]!==255)ay.setEnvelopeShape(r[13]);
 for(let s=0;s<sampleRate/frameRate;s++){ay.process();ay.removeDC();let v=(ay.left+ay.right)/2;audio[at++]=v;peak=Math.max(peak,Math.abs(v));sum+=v*v;}
 if(frame%1250===0)console.log('AY render '+Math.round(frame/frames*100)+'%');
}
const gain=Math.min(.84/peak,.12/Math.sqrt(sum/nSamples)),wav=Buffer.alloc(44+nSamples*4);wav.write('RIFF',0);wav.writeUInt32LE(wav.length-8,4);wav.write('WAVEfmt ',8);wav.writeUInt32LE(16,16);wav.writeUInt16LE(1,20);wav.writeUInt16LE(2,22);wav.writeUInt32LE(sampleRate,24);wav.writeUInt32LE(sampleRate*4,28);wav.writeUInt16LE(4,32);wav.writeUInt16LE(16,34);wav.write('data',36);wav.writeUInt32LE(nSamples*4,40);
for(let i=0;i<audio.length;i++){let value=Math.round(Math.max(-1,Math.min(1,audio[i]*gain))*32767);wav.writeInt16LE(value,44+i*4);wav.writeInt16LE(value,46+i*4);}
fs.writeFileSync(path.join(root,'PaperAndSteam-AY.wav'),wav);
const header=Buffer.alloc(34);header.write('YM6!LeOnArD!');header.writeUInt32BE(frames,12);header.writeUInt32BE(1,16);header.writeUInt16BE(0,20);header.writeUInt32BE(clock,22);header.writeUInt16BE(frameRate,26);header.writeUInt32BE(0,28);header.writeUInt16BE(0,32);
const planes=Buffer.alloc(frames*16);for(let r=0;r<16;r++)for(let f=0;f<frames;f++)planes[r*frames+f]=data[f*16+r];
const ym=Buffer.concat([header,Buffer.from('Paper and Steam\0Pixel Duel Studio\0AY-3-8910, ZX Spectrum 128 clock, 3 voices, no digital samples\0'),planes,Buffer.from('End!')]);fs.writeFileSync(path.join(root,'PaperAndSteam-AY.ym'),ym);
fs.writeFileSync(path.join(root,'AY-registers.bin'),data);
stats.rmsDb=20*Math.log10(Math.sqrt(sum/nSamples)*gain);stats.peakDb=20*Math.log10(peak*gain);stats.samples=nSamples;stats.allRegistersValidated=true;
fs.writeFileSync(path.join(root,'AY-render.json'),JSON.stringify(stats,null,2));
const gameAudio=path.resolve(root,'../UnityProject/Assets/Resources/Audio');if(fs.existsSync(gameAudio))fs.copyFileSync(path.join(root,'PaperAndSteam-AY.wav'),path.join(gameAudio,'PaperAndSteam.wav'));

fs.writeFileSync(path.join(root,'AY — описание.txt'),'БУМАГА И ПАР / AY-ВЕРСИЯ\n\nФинальная запись создана AY-эмулятором Ayumi в режиме AY-3-8910, а не YM2149.\nЧастота чипа 1 773 400 Гц, обновление регистров 50 Гц.\nТри физических канала: A — мелодия, B — бас с аппаратной огибающей,\nC — быстрые арпеджио с переключением на общий шум для перкуссии.\nГромкости 0–15, 12-битные периоды тонов, общий генератор шума.\nБез дополнительных синтезаторов, сэмплированных ударных и эффектов реверберации.\nWAV содержит одинаковый моносигнал в обоих каналах.\n.ym — переносимая запись регистров PSG, пригодная для AY/YM-плееров.\nAY-registers.bin — 16 регистров на каждый кадр 50 Гц.\n\nМелодия развивается: на 0:19 повтор ниже на тон, с 0:57 — низкая середина,\nзатем возвращение темы и нисходящая кода.\n\nИсходная восьмипартийная LMMS-аранжировка сохранена как эскиз;\nфинальная запись в игре — именно трёхканальная AY-версия.\nЭмулятор: https://github.com/alexanderk23/ayumi-js (MIT),\nна основе Ayumi Peter Sovietov: https://github.com/true-grue/ayumi.\n');
console.log(JSON.stringify(stats));
