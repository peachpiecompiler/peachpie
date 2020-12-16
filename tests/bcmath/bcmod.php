<?php

bcscale(0);

echo bcmod( '5',  '3'), PHP_EOL; //  2
echo bcmod( '5', '-3'), PHP_EOL; //  2
echo bcmod('-5',  '3'), PHP_EOL; // -2
echo bcmod('-5', '-3'), PHP_EOL; // -2
