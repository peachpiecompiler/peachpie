<?php
namespace datetime\clone_002;

function test()
{
    $datetime1 = (new \DateTimeImmutable())->modify("+120 minutes");
    $datetime2 = clone $datetime1;

    return $datetime1->getTimestamp() === $datetime2->getTimestamp() ? "equal" : "not_equal";
}

echo test();

echo "Done.";
