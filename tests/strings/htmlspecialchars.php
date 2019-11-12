<?php
namespace strings\htmlspecialchars;

function f($str) {
	echo htmlspecialchars($str, ENT_QUOTES, "UTF-8", true), "\n";
	echo htmlspecialchars($str, ENT_QUOTES, "UTF-8", false), "\n";
	echo htmlspecialchars($str, ENT_COMPAT , "UTF-8", true), "\n";
	echo htmlspecialchars($str, ENT_COMPAT , "UTF-8", false), "\n";
}

foreach (["<a href='test'>Test</a>", "<a href=&quot;test&quot;>&amp;Test&#039;&#039;&lt;&gt;</a>", "&nbsp;"] as $str) {
	f($str);
}
