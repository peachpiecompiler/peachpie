<?php
namespace strings\pack;

echo bin2hex(pack("ccc",-5,"0001x","-8")),"\n";
echo bin2hex(pack("cCsS",1,1,1,1)),"\n";
echo bin2hex(pack("nviI",1,1,1,1)),"\n";
echo bin2hex(pack("lLNV",1,1,1,1)),"\n";
echo bin2hex(pack("fd",1,1)),"\n";

echo bin2hex(pack("H*","abcde")),"\n";
echo bin2hex(pack("h*","abcde")),"\n";
echo bin2hex(pack("H*","abcd")),"\n";
echo bin2hex(pack("h*","abcd")),"\n";
echo bin2hex(pack("H3i","181",5)),"\n";

echo bin2hex(pack("A*","hello")),"\n";
echo bin2hex(pack("a2","hello")),"\n";
echo bin2hex(pack("a10","hello")),"\n";
echo bin2hex(pack("A10","hello")),"\n";

echo bin2hex(pack("nvc*", 0x1234, 0x5678, 65, 66)),"\n"; 
echo bin2hex(pack("x10X5x8x1X2x1X2")),"\n";

echo bin2hex(pack("@5s2c3","+5e10","007xasd","-6","49",".1")),"\n";
echo bin2hex(pack("@5f2c3","+5e10","007xasd","-6","49",".1")),"\n";

// echo bin2hex(pack("a*","ěščřžýáíé")),"\n"; // TODO: multi byte character support in pack()
echo bin2hex(pack("a0","xxx")),"\n";

list(,$unpacked) = unpack("s*", pack("s*", 123));
echo $unpacked,"\n";

$b=unpack("H3/ias",pack("H3i","181",5));
echo count($b),"\n",$b[1],"\n",$b["as"],"\n";	
