<?php
namespace tests\process;

/**
 * @group process
 * @group proc_open
 */
echo "proc_open test\n";

$descriptor = array(
    0 => array('pipe', 'r'),
    1 => array('pipe', 'w'),
);

$pipes = null;
$process = proc_open("true", $descriptor, $pipes);

if (is_resource($process)) {
    echo "process opened: OK\n";
    fclose($pipes[0]);
    fclose($pipes[1]);
    $code = proc_close($process);
    echo "exit code: $code\n";
} else {
    echo "failed to open process\n";
}

echo "done\n";
