<?php
namespace constructs\exit_001;

register_shutdown_function(function () { exit(42); });
