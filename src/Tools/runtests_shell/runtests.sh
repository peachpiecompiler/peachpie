# WARNING: This script file manipulates with /bin/less command (see below), it is recommended to run it in a virtual sandbox 
#          in order not to harm anything. The script was written for automated tests in Travis CI.

# Prepare the files needed to compile and run the tests

TOOL_DIR="./src/Tools/runtests_shell"
OUTPUT_DIR="$TOOL_DIR/bin"

cp src/Peachpie.Runtime/bin/Debug/netcoreapp1.0/Peachpie.Runtime.dll $OUTPUT_DIR
cp src/Peachpie.Library/bin/Debug/netcoreapp1.0/Peachpie.Library.dll $OUTPUT_DIR

# The location of the referenced libraries may differ and the compiler works properly only with absolute addresses
NUGET_DIR="$(readlink -f ~/.nuget/packages)"
awk "{print \"--reference:$NUGET_DIR/\" \$0}" $TOOL_DIR/references.rsp.tpl > $TOOL_DIR/references.rsp

COMPILE_PHP_DLL="./src/Peachpie.Compiler.Tools/bin/Debug/netcoreapp1.0/dotnet-compile-php.dll"
COMPILE_PHP="dotnet $COMPILE_PHP_DLL --temp-output:$OUTPUT_DIR --out:$OUTPUT_DIR/output.exe @$TOOL_DIR/common.rsp @$TOOL_DIR/references.rsp"

PHP_TMP_FILE=$OUTPUT_DIR/php.out
PEACH_TMP_FILE=$OUTPUT_DIR/peach.out

COLOR_GREEN="\033[1;32m"
COLOR_RED="\033[1;31m"
COLOR_RESET="\033[0m"
HR="----------------------------------------------------------------------------------------------------------------------------------------------------------------"

# A dirty hack to force cdiff to print the result in a non-interactive mode
sudo cp /bin/less $TOOL_DIR/less_bck
sudo rm /bin/less
sudo ln $TOOL_DIR/cdiff_less_replacement.sh /bin/less

# Compile and run every PHP file in ./tests and check the output against the one from the PHP interpreter
for PHP_FILE in $(find ./tests -name *.php)
do
  echo -n "Testing $PHP_FILE..."
  COMPILE_OUTPUT="$($COMPILE_PHP $PHP_FILE 2>&1)"
  if [ $PIPESTATUS != 0 ] ; then
    echo -e $COLOR_RED"Compilation error"$COLOR_RESET
    echo "$COMPILE_OUTPUT"
    FAILURE="FAILURE"
  else
    PHP_OUTPUT="$(php $PHP_FILE)"
    PEACH_OUTPUT="$(dotnet $OUTPUT_DIR/output.exe)"

    if [ "$PHP_OUTPUT" = "$PEACH_OUTPUT" ] ; then
      echo -e $COLOR_GREEN"OK"$COLOR_RESET
    else
      echo -e $COLOR_RED"FAIL"$COLOR_RESET
      echo "Differences between the expected and actual result:"
      echo $HR
      echo "$PHP_OUTPUT" > $PHP_TMP_FILE
      echo "$PEACH_OUTPUT" > $PEACH_TMP_FILE
      # TODO: Hide the whole comparison header (tail after the cdiff won't work)
      git diff --no-index -- $PHP_TMP_FILE $PEACH_TMP_FILE | tail -n +3 | cdiff -s
      echo $HR
      FAILURE="FAILURE"
    fi
  fi
done

# Revert the hack of command less
sudo rm /bin/less
sudo cp $TOOL_DIR/less_bck /bin/less
sudo rm $TOOL_DIR/less_bck

# Fail if any of the tests failed
if [ $FAILURE ] ; then
  echo -e $COLOR_RED"Tests failed"$COLOR_RESET
  exit 1
else
  echo -e $COLOR_GREEN"Tests passed"$COLOR_RESET
  exit 0
fi