<?php
namespace gettext\gettext_001;

// Neither setlocale nor LC_ALL= works on Travis CI
if (getenv("TRAVIS")) {
    exit("***SKIP***");
}

function translate() {
  echo _("Hi");
  echo " ";
  echo gettext("Goodbye");
  echo " ";
  echo ngettext("Bug", "Bugs", 1);
  echo " ";
  echo dgettext("gettext_001_alternate", "Hi");
  echo " ";
  echo dngettext("gettext_001_alternate", "Bug", "Bugs", 2);
  echo " ";
  echo dngettext("gettext_001_alternate", "Bug", "Bugs", 10);
  echo " ";
  echo dcgettext("gettext_001_alternate", "Goodbye", LC_MONETARY);
  echo " ";
  echo dcngettext("gettext_001_alternate", "Bug", "Bugs", 42, LC_NUMERIC);
}

function test($locale, $domain, $dir) {
  if ($locale) {
    putenv("LC_ALL=" . $locale);
    setlocale(LC_ALL, $locale);
  }

  if ($dir) {
    $res = bindtextdomain($domain ?: "messages", $dir);
    echo ($res === false) ? "false" : strtolower($res);
    echo " ";
  }

  echo textdomain($domain);
  echo " ";

  translate();
  echo "\n";
}

test("en_GB", null, null);
test(null, "nonexisting_domain", null);
test(null, null, "nonexisting_dir");
test(null, "gettext_001_invalid", "translations");
test(null, "gettext_001_default", "translations");
test("cs_CZ", null, null);
test("cs_CZ", "gettext_001_alternate", "translations");
test(null, "gettext_001_alternate", null);
test("sk_SK", "gettext_001_default", "translations");
test("cs_CZ", null, null);
test(null, "gettext_001_default", "translations2");
