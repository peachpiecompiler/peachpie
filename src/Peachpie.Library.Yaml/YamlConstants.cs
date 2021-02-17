using System;
using System.Collections.Generic;
using System.Text;
using Pchp.Core;

namespace Peachpie.Library.Yaml
{
    [PhpExtension(YamlExtension.Name)]
    public static class YamlConstants
    {
        //Scalar entity styles usable by yaml_parse() callback methods.

        //YAML_ANY_SCALAR_STYLE(int)
        //YAML_PLAIN_SCALAR_STYLE(int)
        //YAML_SINGLE_QUOTED_SCALAR_STYLE(int)
        //YAML_DOUBLE_QUOTED_SCALAR_STYLE(int)
        //YAML_LITERAL_SCALAR_STYLE(int)
        //YAML_FOLDED_SCALAR_STYLE(int)

        //Common tags usable by yaml_parse() callback methods.

        //"tag:yaml.org,2002:null"
        //YAML_NULL_TAG(string)
        //"tag:yaml.org,2002:bool"
        //YAML_BOOL_TAG(string)
        //"tag:yaml.org,2002:str"
        //YAML_STR_TAG(string)
        //"tag:yaml.org,2002:int"
        //YAML_INT_TAG(string)
        //"tag:yaml.org,2002:float"
        //YAML_FLOAT_TAG(string)
        //"tag:yaml.org,2002:timestamp"
        //YAML_TIMESTAMP_TAG(string)
        //"tag:yaml.org,2002:seq"
        //YAML_SEQ_TAG(string)
        //"tag:yaml.org,2002:map"
        //YAML_MAP_TAG(string)
        //"!php/object"
        //YAML_PHP_TAG(string)

        //Encoding types for yaml_emit()

        //Let the emitter choose an encoding.
        //YAML_ANY_ENCODING(int)
        //Encode as UTF8.
        //YAML_UTF8_ENCODING(int)
        //Encode as UTF16LE.
        //YAML_UTF16LE_ENCODING(int)
        //Encode as UTF16BE.
        //YAML_UTF16BE_ENCODING(int)

        //Linebreak types for yaml_emit()

        //Let emitter choose linebreak character.
        //YAML_ANY_BREAK(int)
        //Use \r as break character(Mac style).
        //YAML_CR_BREAK(int)
        //Use \n as break character(Unix style).
        //YAML_LN_BREAK(int)
        //Use \r\n as break character(DOS style).
        //YAML_CRLN_BREAK(int)

    }
}
