// Copyright 2018 The Nakama Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

package main

import (
	"bufio"
	"encoding/json"
	"flag"
	"fmt"
	"io/ioutil"
	"os"
	"strings"
	"text/template"
)

const codeTemplate string = `/* Code generated by openapi-gen/main.go. DO NOT EDIT. */

namespace Nakama
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading.Tasks;
    using TinyJson;

    {{- range $defname, $definition := .Definitions }}
    {{- $classname := $defname | title }}

    /// <summary>
    /// {{ $definition.Description | stripNewlines }}
    /// </summary>
    public interface I{{ $classname }}
    {
        {{- range $propname, $property := $definition.Properties }}
        {{- $fieldname := $propname | pascalCase }}

        /// <summary>
        /// {{ $property.Description }}
        /// </summary>
        {{- if eq $property.Type "integer"}}
        int {{ $fieldname }} { get; }
        {{- else if eq $property.Type "boolean" }}
        bool {{ $fieldname }} { get; }
        {{- else if eq $property.Type "string"}}
        string {{ $fieldname }} { get; }
        {{- else if eq $property.Type "array"}}
            {{- if eq $property.Items.Type "string"}}
        List<string> {{ $fieldname }} { get; }
            {{- else if eq $property.Items.Type "integer"}}
        List<int> {{ $fieldname }} { get; }
            {{- else if eq $property.Items.Type "boolean"}}
        List<bool> {{ $fieldname }} { get; }
            {{- else}}
        IEnumerable<I{{ $property.Items.Ref | cleanRef }}> {{ $fieldname }} { get; }
            {{- end }}
        {{- else }}
        I{{ $property.Ref | cleanRef }} {{ $fieldname }} { get; }
        {{- end }}
        {{- end }}
    }

    /// <inheritdoc />
    internal class {{ $classname }} : I{{ $classname }}
    {
        {{- range $propname, $property := $definition.Properties }}
        {{- $fieldname := $propname | pascalCase }}

        /// <inheritdoc />
        {{- if eq $property.Type "integer" }}
        [DataMember(Name="{{ $propname }}")]
        public int {{ $fieldname }} { get; set; }
        {{- else if eq $property.Type "boolean" }}
        [DataMember(Name="{{ $propname }}")]
        public bool {{ $fieldname }} { get; set; }
        {{- else if eq $property.Type "string" }}
        [DataMember(Name="{{ $propname }}")]
        public string {{ $fieldname }} { get; set; }
        {{- else if eq $property.Type "array" }}
            {{- if eq $property.Items.Type "string" }}
        [DataMember(Name="{{ $propname }}")]
        public List<string> {{ $fieldname }} { get; set; }
            {{- else if eq $property.Items.Type "integer" }}
        [DataMember(Name="{{ $propname }}")]
        public List<int> {{ $fieldname }} { get; set; }
            {{- else if eq $property.Items.Type "boolean" }}
        [DataMember(Name="{{ $propname }}")]
        public List<bool> {{ $fieldname }} { get; set; }
            {{- else}}
        public IEnumerable<I{{ $property.Items.Ref | cleanRef }}> {{ $fieldname }} => _{{ $propname | camelCase }} ?? new List<{{ $property.Items.Ref | cleanRef }}>(0);
        [DataMember(Name="{{ $propname }}")]
        public List<{{ $property.Items.Ref | cleanRef }}> _{{ $propname | camelCase }} { get; set; }
            {{- end }}
        {{- else }}
        public I{{ $property.Ref | cleanRef }} {{ $fieldname }} => _{{ $propname | camelCase }};
        [DataMember(Name="{{ $propname }}")]
        public {{ $property.Ref | cleanRef }} _{{ $propname | camelCase }} { get; set; }
        {{- end }}
        {{- end }}

        public override string ToString()
        {
            var output = "";
            {{- range $fieldname, $property := $definition.Properties }}
            {{- if eq $property.Type "array" }}
            output = string.Concat(output, "{{ $fieldname | pascalCase }}: [", string.Join(", ", {{ $fieldname | pascalCase }}), "], ");
            {{- else }}
            output = string.Concat(output, "{{ $fieldname | pascalCase }}: ", {{ $fieldname | pascalCase }}, ", ");
            {{- end }}
            {{- end }}
            return output;
        }
    }
    {{- end }}

    /// <summary>
    /// </summary>
    public interface IRequestDispatcher {
    }

    /// <summary>
    /// The low level client for the Nakama API.
    /// </summary>
    internal class ApiClient
    {
        private readonly IRequestDispatcher _dispatcher;
        private readonly Uri _baseUri;

        public ApiClient(IRequestDispatcher dispatcher, Uri baseUri)
        {
        	_dispatcher = dispatcher;
        	_baseUri = baseUri;
        }

        {{- range $url, $path := .Paths }}
        {{- range $method, $operation := $path}}

        /// <summary>
        /// {{ $operation.Summary | stripNewlines }}
        /// </summary>
        public async Task<I{{ $operation.Responses.Ok.Schema.Ref | cleanRef }}> {{ $operation.OperationId | pascalCase }}Async(
        {{- if $operation.Security }}
        {{- with (index $operation.Security 0) }}
            {{- range $key, $value := . }}
                {{- if eq $key "BasicAuth" }}
            string username
            , string password
                {{- else if eq $key "HttpKeyAuth" }}
            string bearerToken
                {{- end }}
            {{- end }}
        {{- end }}
        {{- else }}
            string bearerToken
        {{- end }}

        {{- range $parameter := $operation.Parameters }}
        {{- $camelcase := $parameter.Name | camelCase }}
        {{- if eq $parameter.In "path" }}
            , {{ $parameter.Type }}{{- if not $parameter.Required }}?{{- end }} {{ $camelcase }}
        {{- else if eq $parameter.In "body" }}
            {{- if eq $parameter.Schema.Type "string" }}
            , string{{- if not $parameter.Required }}?{{- end }} {{ $camelcase }}
            {{- else }}
            , {{ $parameter.Schema.Ref | cleanRef }}{{- if not $parameter.Required }}?{{- end }} {{ $camelcase }}
            {{- end }}
        {{- else if eq $parameter.Type "array"}}
            , IEnumerable<{{ $parameter.Items.Type }}> {{ $camelcase }}
        {{- else if eq $parameter.Type "integer" }}
            , int {{ $camelcase }}
        {{- else if eq $parameter.Type "boolean" }}
            , bool {{ $camelcase }}
        {{- else }}
            , {{ $parameter.Type }} {{ $camelcase }}
        {{- end }}
        {{- end }})
        {
        	HttpClient client = new HttpClient(); // FIXME
            {{- range $parameter := $operation.Parameters }}
            {{- $camelcase := $parameter.Name | camelCase }}
            {{- if $parameter.Required }}
            if ({{ $camelcase }} == null)
            {
                throw new ArgumentException("'{{ $camelcase }}' is required but was null.");
            }
            {{- end }}
            {{- end }}

            var urlpath = "{{- $url }}?";
            {{- range $parameter := $operation.Parameters }}
            {{- $camelcase := $parameter.Name | camelCase }}
            {{- if eq $parameter.In "path" }}
            urlpath = urlpath.Replace("{{- print "{" $parameter.Name "}"}}", Uri.EscapeDataString({{- $camelcase }}));
            {{- end }}
            {{- end }}

            {{- range $parameter := $operation.Parameters }}
            {{- $camelcase := $parameter.Name | camelCase }}
            {{- if eq $parameter.In "query"}}
                {{- if eq $parameter.Type "integer" }}
            urlpath = string.Concat(urlpath, "{{- $parameter.Name }}=", {{ $camelcase }}, "&");
                {{- else if eq $parameter.Type "string" }}
            urlpath = string.Concat(urlpath, "{{- $parameter.Name }}=", Uri.EscapeDataString({{ $camelcase }}), "&");
                {{- else if eq $parameter.Type "boolean" }}
            urlpath = string.Concat(urlpath, "{{- $parameter.Name }}=", {{ $camelcase }}.ToString().ToLower(), "&");
                {{- else if eq $parameter.Type "array" }}
            foreach (var elem in {{ $camelcase }})
            {
                urlpath = string.Concat(urlpath, "{{- $parameter.Name }}=", elem, "&");
            }
                {{- else }}
            {{ $parameter }} // ERROR
                {{- end }}
            {{- end }}
            {{- end }}

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(_baseUri, urlpath),
                Method = new HttpMethod("{{- $method | uppercase }}"),
                Headers =
                {
                    Accept = {new MediaTypeWithQualityHeaderValue("application/json")}
                }
            };

            {{- if $operation.Security }}
            {{- with (index $operation.Security 0) }}
                {{- range $key, $value := . }}
                    {{- if eq $key "BasicAuth" }}
            var credentials = Encoding.UTF8.GetBytes(username + ":" + password);
            var header = string.Concat("Basic ", Convert.ToBase64String(credentials));
            request.Headers.Authorization = AuthenticationHeaderValue.Parse(header);
                    {{- else if eq $key "HttpKeyAuth" }}
            if (!string.IsNullOrEmpty(bearerToken))
            {
                var header = string.Concat("Bearer ", bearerToken);
                request.Headers.Authorization = AuthenticationHeaderValue.Parse(header);
            }
                    {{- end }}
                {{- end }}
            {{- end }}
            {{- else }}
            var header = string.Concat("Bearer ", bearerToken);
            request.Headers.Authorization = AuthenticationHeaderValue.Parse(header);
            {{- end }}

            {{- range $parameter := $operation.Parameters }}
            {{- $camelcase := $parameter.Name | camelCase }}
            {{- if eq $parameter.In "body" }}
            request.Content = new StringContent({{ $camelcase }}.ToJson());
            {{- end }}
            {{- end }}

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var contents = await response.Content.ReadAsStringAsync();
            return contents.FromJson<{{ $operation.Responses.Ok.Schema.Ref | cleanRef }}>();
        }
        {{- end }}
        {{- end }}
    }
}
`

func convertRefToClassName(input string) (className string) {
	cleanRef := strings.TrimLeft(input, "#/definitions/")
	className = strings.Title(cleanRef)
	return
}

func snakeCaseToCamelCase(input string) (camelCase string) {
	isToUpper := false
	for k, v := range input {
		if k == 0 {
			camelCase = strings.ToLower(string(input[0]))
		} else {
			if isToUpper {
				camelCase += strings.ToUpper(string(v))
				isToUpper = false
			} else {
				if v == '_' {
					isToUpper = true
				} else {
					camelCase += string(v)
				}
			}
		}

	}
	return
}

func snakeCaseToPascalCase(input string) (output string) {
	isToUpper := false
	for k, v := range input {
		if k == 0 {
			output = strings.ToUpper(string(input[0]))
		} else {
			if isToUpper {
				output += strings.ToUpper(string(v))
				isToUpper = false
			} else {
				if v == '_' {
					isToUpper = true
				} else {
					output += string(v)
				}
			}
		}
	}
	return
}

func stripNewlines(input string) (output string) {
	output = strings.Replace(input, "\n", " ", -1)
	return
}

func main() {
	// Argument flags
	var output = flag.String("output", "", "The output for generated code.")
	flag.Parse()

	inputs := flag.Args()
	if len(inputs) < 1 {
		fmt.Printf("No input file found: %s\n\n", inputs)
		fmt.Println("openapi-gen [flags] inputs...")
		flag.PrintDefaults()
		return
	}

	input := inputs[0]
	content, err := ioutil.ReadFile(input)
	if err != nil {
		fmt.Printf("Unable to read file: %s\n", err)
		return
	}

	var schema struct {
		Paths map[string]map[string]struct {
			Summary     string
			OperationId string
			Responses   struct {
				Ok struct {
					Schema struct {
						Ref string `json:"$ref"`
					}
				} `json:"200"`
			}
			Parameters []struct {
				Name     string
				In       string
				Required bool
				Type     string   // used with primitives
				Items    struct { // used with type "array"
					Type string
				}
				Schema struct { // used with http body
					Type string
					Ref  string `json:"$ref"`
				}
			}
			Security []map[string][]struct {
			}
		}
		Definitions map[string]struct {
			Properties map[string]struct {
				Type  string
				Ref   string   `json:"$ref"` // used with object
				Items struct { // used with type "array"
					Type string
					Ref  string `json:"$ref"`
				}
				Format      string // used with type "boolean"
				Description string
			}
			Description string
		}
	}

	if err := json.Unmarshal(content, &schema); err != nil {
		fmt.Printf("Unable to decode input %s : %s\n", input, err)
		return
	}

	fmap := template.FuncMap{
		"camelCase":     snakeCaseToCamelCase,
		"cleanRef":      convertRefToClassName,
		"pascalCase":    snakeCaseToPascalCase,
		"stripNewlines": stripNewlines,
		"title":         strings.Title,
		"uppercase":     strings.ToUpper,
	}
	tmpl, err := template.New(input).Funcs(fmap).Parse(codeTemplate)
	if err != nil {
		fmt.Printf("Template parse error: %s\n", err)
		return
	}

	if len(*output) < 1 {
		tmpl.Execute(os.Stdout, schema)
		return
	}

	f, err := os.Create(*output)
	if err != nil {
		fmt.Printf("Unable to create file: %s\n", err)
		return
	}
	defer f.Close()

	writer := bufio.NewWriter(f)
	tmpl.Execute(writer, schema)
	writer.Flush()
}
