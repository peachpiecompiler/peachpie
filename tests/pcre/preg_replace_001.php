<?php
namespace pcre\preg_replace_001;

function underscore($camelCasedWord) {
	return strtolower(preg_replace('/(?<=\\w)([A-Z])/', '_\\1', $camelCasedWord));
}

echo underscore("AbcDef");
echo underscore("abcDefGhi");
echo underscore("AbcDef_word");
echo underscore("text1");
echo underscore("text_1");

echo "Done.";
