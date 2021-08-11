<?php
namespace pcre\preg_match_006;

preg_match('/(?P<fooname>foo)?(bar)/', 'bar', $matches, PREG_OFFSET_CAPTURE);

$is_fooname = isset( $matches['fooname'] ) && -1 !== $matches['fooname'][1];

echo "is_fooname:" . ($is_fooname ? "true" : "false") . "\n"; //is_fooname:false