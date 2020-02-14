#!/bin/bash
# Prepare the files needed to compile and run the tests

# Absolute paths - https://stackoverflow.com/a/246128/2105235
TESTS_DIR=$PWD/tests
OUTPUT_DIR="$TESTS_DIR/bin/Release/netcoreapp3.0"

PHP_TMP_FILE=$OUTPUT_DIR/php.out
PEACH_TMP_FILE=$OUTPUT_DIR/peach.out

COLOR_YELLOW="\033[1;33m"
COLOR_GREEN="\033[1;32m"
COLOR_RED="\033[1;31m"
COLOR_RESET="\033[0m"
HR="----------------------------------------------------------------------------------------------------------------------------------------------------------------"

# Run every PHP test with Peachpie (we expect them to be already compiled) and check the output against PHP interpreter
for PHP_FILE in $(find $TESTS_DIR -name *.php)
do
  # Run each file in the directory it is contained in (in order for relative paths to work)
  PHP_FILE_DIR=$(dirname $PHP_FILE)
  cd $PHP_FILE_DIR

  # Obtain the relative path to the PHP file so that the inclusion in Peachpie works properly
  CUT_START=$((${#TESTS_DIR} + 2))
  PHP_FILE_REL=$(echo $PHP_FILE | cut -c $CUT_START-)

  echo -n "Testing $PHP_FILE_REL..."

  # Skip the test according to its filename ..
  echo "$PHP_FILE" | grep -Eq ".*skip(\\([^)/]*\\))?_[^/]*$"
  if [ $? -eq 0 ];then
    echo -e $COLOR_YELLOW"SKIPPED"$COLOR_RESET
    continue;
  fi

  PHP_OUTPUT="$(php -d display_errors=Off -d log_errors=Off $PHP_FILE)"
  PEACH_OUTPUT="$(dotnet $OUTPUT_DIR/Tests.dll $PHP_FILE_REL)"

  # .. or if either Peachpie or PHP returned a special string
  if [ "$PHP_OUTPUT" = "***SKIP***" -o "$PEACH_OUTPUT" = "***SKIP***" ] ; then
    echo -e $COLOR_YELLOW"SKIPPED"$COLOR_RESET
    continue;
  fi

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
done

# Fail if any of the tests failed
if [ $FAILURE ] ; then
  echo -e $COLOR_RED"Tests failed"$COLOR_RESET
  exit 1
else
  echo -e $COLOR_GREEN"Tests passed"$COLOR_RESET
  exit 0
fi
