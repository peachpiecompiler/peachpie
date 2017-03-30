# Prepare the files needed to compile and run the tests

TOOL_DIR="./src/Tools/runtests_shell"
OUTPUT_DIR="$TOOL_DIR/bin/Debug/netcoreapp1.0"

PHP_TMP_FILE=$OUTPUT_DIR/php.out
PEACH_TMP_FILE=$OUTPUT_DIR/peach.out

COLOR_GREEN="\033[1;32m"
COLOR_RED="\033[1;31m"
COLOR_RESET="\033[0m"
HR="----------------------------------------------------------------------------------------------------------------------------------------------------------------"

# Restore the testing project to gather all dependencies
dotnet restore $TOOL_DIR

# Compile and run every PHP file in ./tests and check the output against the one from the PHP interpreter
for PHP_FILE in $(find $PWD/tests -name *.php)
do
  echo -n "Testing $PHP_FILE..."
  COMPILE_OUTPUT="$(dotnet build $TOOL_DIR /p:TestFile=$PHP_FILE)"
  if [ $PIPESTATUS != 0 ] ; then
    echo -e $COLOR_RED"Compilation error"$COLOR_RESET
    echo "$COMPILE_OUTPUT"
    FAILURE="FAILURE"
  else
    PHP_OUTPUT="$(php $PHP_FILE)"
    PEACH_OUTPUT="$(dotnet $OUTPUT_DIR/Test.dll)"

    if [ "$PHP_OUTPUT" = "$PEACH_OUTPUT" ] ; then
      echo -e $COLOR_GREEN"OK"$COLOR_RESET
    else
      echo -e $COLOR_RED"FAIL"$COLOR_RESET
      echo "Differences between the expected and actual result:"
      echo $HR
      echo "$PHP_OUTPUT" > $PHP_TMP_FILE
      echo "$PEACH_OUTPUT" > $PEACH_TMP_FILE
      icdiff --line-numbers --cols=160 --no-headers --show-all-spaces $PHP_TMP_FILE $PEACH_TMP_FILE
      echo $HR
      FAILURE="FAILURE"
    fi
  fi
done

# Fail if any of the tests failed
if [ $FAILURE ] ; then
  echo -e $COLOR_RED"Tests failed"$COLOR_RESET
  exit 1
else
  echo -e $COLOR_GREEN"Tests passed"$COLOR_RESET
  exit 0
fi
