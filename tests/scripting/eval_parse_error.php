<?php

try { eval("<"); } catch (ParseError $e) { echo "caught ", get_class($e), "\n"; }
try { eval("function ("); } catch (Throwable $e) { echo "caught ", get_class($e), "\n"; }
echo "done\n";
