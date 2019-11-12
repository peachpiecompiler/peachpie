<?php
namespace strings\str_ireplace;

function da($a)
{
  foreach($a as $k => $v) echo "$k => $v\n";
}

echo str_replace("","as","asxas"),"\n";

$a = array("aLEP","be","th","mm");
$b = array("bet","he","he");
$c = array("aleph","beth","gimmel");
da(str_ireplace($a,$b,$c));


$a = array("aLEP","be","th","mm");
$b = array("bet","he","he");
$c = array("aleph","beth","gimmel");
da(str_replace($a,$b,$c));

da(str_ireplace(array (
                                   '*',
                                   'SELECT ',
                                   'UPDATE ',
                                   'DELETE ',
                                   'INSERT ',
                                   'INTO',
                                   'VALUES',
                                   'FROM',
                                   'LEFT',
                                   'JOIN',
                                   'WHERE',
                                   'LIMIT',
                                   'ORDER BY',
                                   'AND',
                                   'OR ',
                                   'DESC',
                                   'ASC',
                                   'ON '
                                 ),
                           array (
                                   "<FONT COLOR='#FF6600'><B>*</B></FONT>",
                                   "<FONT COLOR='#00AA00'><B>SELECT</B> </FONT>",
                                   "<FONT COLOR='#00AA00'><B>UPDATE</B> </FONT>",
                                   "<FONT COLOR='#00AA00'><B>DELETE</B> </FONT>",
                                   "<FONT COLOR='#00AA00'><B>INSERT</B> </FONT>",
                                   "<FONT COLOR='#00AA00'><B>INTO</B></FONT>",
                                   "<FONT COLOR='#00AA00'><B>VALUES</B></FONT>",
                                   "<FONT COLOR='#00AA00'><B>FROM</B></FONT>",
                                   "<FONT COLOR='#00CC00'><B>LEFT</B></FONT>",
                                   "<FONT COLOR='#00CC00'><B>JOIN</B></FONT>",
                                   "<FONT COLOR='#00AA00'><B>WHERE</B></FONT>",
                                   "<FONT COLOR='#AA0000'><B>LIMIT</B></FONT>",
                                   "<FONT COLOR='#00AA00'><B>ORDER BY</B></FONT>",
                                   "<FONT COLOR='#0000AA'><B>AND</B></FONT>",
                                   "<FONT COLOR='#0000AA'><B>OR</B> </FONT>",
                                   "<FONT COLOR='#0000AA'><B>DESC</B></FONT>",
                                   "<FONT COLOR='#0000AA'><B>ASC</B></FONT>",
                                   "<FONT COLOR='#00DD00'><B>ON</B> </FONT>"
                                 ),
                           array(
                            "Select * from adsad where asdasdasda order by by order by limit 21 on asc descwhere",
                            "select from upsdateinsertasc delete join axax"),
                            $count
                         ));
echo $count,"\n";

echo bin2hex(str_ireplace("\r\n","-","\r\nhell\r\n\r\n\r\no\r\nw\r\n",$c1));
echo bin2hex(str_replace("\r\n","-","h\r\nel\r\nlow\r\r\n\r\n\n",$c2));
echo $c1,"-",$c2;
