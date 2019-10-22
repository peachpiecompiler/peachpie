<?php

namespace
{
    const C1 = 12;
    echo C1;
}

namespace constants\constants_002
{
    echo C1;    // global constant, lookups both /A/C1 and /C1
    const C1 = 34;
    const C2 = 56;
    echo C1;
    echo C2;

    echo "\nE_ERROR:", E_ERROR, "\n";   // global const

    echo "Done.";
}
