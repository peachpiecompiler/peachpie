<?php
namespace strings\heredoc; 

$test = <<<EOB
line
EOB;

echo ">>>$test<<<", PHP_EOL;
echo "Done.";