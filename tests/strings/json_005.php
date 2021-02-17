<?php
namespace variables\json_005;

\json_encode(NAN, JSON_PARTIAL_OUTPUT_ON_ERROR);
echo "last error: ", \json_last_error(), PHP_EOL; // != 0

\json_encode("", JSON_PARTIAL_OUTPUT_ON_ERROR);
echo "last error: ", \json_last_error(), PHP_EOL; // == 0

echo "Done.";
