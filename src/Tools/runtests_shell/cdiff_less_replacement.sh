#!/bin/sh

# A dirty hack to force cdiff to print the result in a non-interactive mode by
# temporarily replacing /bin/less command (see runtests.sh)
cat - | tail -n +4