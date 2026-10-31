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
$process = proc_open("exit 0", $descriptor, $pipes);

if (is_resource($process)) {
    echo "process opened: OK\n";
    fclose($pipes[0]);
    fclose($pipes[1]);
    $code = proc_close($process);
    echo "exit code: $code\n";
} else {
    echo "failed to open process\n";
}

// Test that commands with quoted arguments containing spaces are passed correctly
// to the shell. This exercises the ArgumentList path on Linux/macOS.
if (PHP_OS_FAMILY !== 'Windows') {
    $pipes2 = null;
    $process2 = proc_open("printf '%s|' 'a b' c", [1 => ['pipe', 'w']], $pipes2);
    if (is_resource($process2)) {
        $output = stream_get_contents($pipes2[1]);
        fclose($pipes2[1]);
        $code2 = proc_close($process2);
        echo "printf output: $output\n";  // expected: a b|c|
        if ($output !== "a b|c|") echo "FAIL\n";
        echo "printf exit code: $code2\n";  // expected: 0
        if ($code2 !== 0) echo "FAIL\n";
    } else {
        echo "failed to open process for printf test\n";
    }
}

echo "done\n";
