<?php
namespace pcre\preg_filter;

\print_r(\preg_filter(['/quick/', '/brown/'], "x", "quick fox"));
\print_r(\preg_filter(['/quick/', '/brown/'], "x", ["fox", "brown fox"]));
\print_r(\preg_filter(['/quick/', '/brown/'], "x", ["fox", "red fox"]));

echo "Done.";
