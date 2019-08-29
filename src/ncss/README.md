# NCss is a small library aiming to parse and manipulate css sheets.

[![Build status](https://ci.appveyor.com/api/projects/status/6gmqjtw0gvkuggme?svg=true)](https://ci.appveyor.com/project/oguimbal/ncss)

[Nuget package](https://www.nuget.org/packages/NCss)
```
  Install-Package NCss
```

# Features

- Parses invalid CSS without failing
- Detects hacks (* hack, \9 hack, \0 hack) [see Property.cs @70](https://github.com/oguimbal/ncss/blob/master/NCss/Parsers/Property.cs#L70)
- Easy to search & modify parsed CSS
- Build CSS sheet from parsed/modified (or manually built) CSS tree
- CSS sanitilization
- Detailed parsing errors
- Building sheets programmatically

# Disclaimer
I tried to focus on having a performant parser which does not create hundreds of thousands of substrings of the same string while parsing.

NCss is not (yet?) aiming to be a strict and full-feature-browser-ready CSS parser (it skips some value parsing, for instance it only does some very basic value checking)... it's rather a parser that helps to manipulate and build sheets.
But you're free to contribute :)

You will not see any link to W3 doc or equivalent. 
I'm not based on anything else apart my common sense, and a lot of real-life UTs: 
**NCss is tested against complex CSS sheets** such as [boostrap](http://getbootstrap.com/) - among others. See [RealTests.css](https://github.com/oguimbal/ncss/blob/master/NCss.Tests/RealTests.cs).

I know that the parsing method is a bit unorthodox (not the usual lexer/parser structure) but CSS is a simple language, so i've tried to keep things simple for the parser as well, which makes things far more easy to debug.
... you'll know what I mean if you try to debug NCss using the good old F10/F11 method.

A small warning: The "search" functionality is not very optimized. 
It's a lot of linq, with a lot of deffered executions, and thus a lot of state machines behind the scene.
... seems to be quite ok for me so far, though.

# Usage samples

You can find several example in unit tests.
For instance:

### Remove a class
```C#
var sheet = new CssParser().ParseSheet(".cl1{}.cl2#id{}");
foreach (var f in sheet.Find<ClassRule>(x => x.Selector == ".cl2#id").ToArray())
    f.Remove();
Assert.True(sheet.IsValid);
Assert.AreEqual(".cl1{}", sheet.ToString());
```

### Modify a selector
```C#
  var sheet = new CssParser().ParseSheet(".cl:hover,.cl2:hover{}.cl3:hover{}");
  foreach (var f in sheet.Find<SimpleSelector>(x => x.FullName == ":hover").ToArray())
      f.Remove();
  Assert.True(sheet.IsValid);
  Assert.AreEqual(".cl,.cl2{}.cl3{}", sheet.ToString());
```

### Remove a property
```C#
  var sheet = new CssParser().ParseSheet(".cl1{prop:red;other:null;}.cl2#id{prop:test;}");
  foreach (var f in sheet.Find<Property>(x => x.Name == "prop").ToArray())
      f.Remove();
  Assert.True(sheet.IsValid);
  Assert.AreEqual(".cl1{other:null;}.cl2#id{}", sheet.ToString());
```

### Replace an url
```C#
  var sheet = new CssParser().ParseSheet(".cl{background-image:url(test.png)}");
  foreach (var f in sheet.Find<CssSimpleValue>(x => x.IsFunction && x.Name == "url").ToArray())
      f.ReplaceBy(new CssSimpleValue("url", "other.png"));
  Assert.True(sheet.IsValid);
  Assert.AreEqual(".cl{background-image:url(other.png);}", sheet.ToString());
```


### Sanitizing CSS

```C#
  var p = new CssParser().ParseSheet(".class{prop:#ffff;msldkqj;}");
  // this will reconstitute the original CSS that has been parsed (even if invalid css)
  Assert.AreEqual(".class{prop:#ffff;msldkqj;}", p.ToString(CssRestitution.OriginalWhenErrorOrInvalid));
  // this will reconstitue somwhat valid css, even if it may have invalid property values
  Assert.AreEqual(".class{prop:#ffff;}", p.ToString(CssRestitution.RemoveErrors));
  // this will ensure valid css
  Assert.AreEqual(".class{}", p.ToString(CssRestitution.RemoveInvalid));
```

### CSS parsing errors

```C#
  var p = new CssParser().ParseSheet(".validClass{}{color:blue;}.class{color:red;}");
  // check that error has been detected
  Assert.AreEqual(1,parser.Errors.Count);
  Assert.False(p.IsValid);
  // check that reconstitution removes this error
  Assert.AreEqual(".validClass{}.class{color:red;}", p.ToString());
  // ... but you can reconstitue the css with the original error as well if you wish
  Assert.AreEqual(".validClass{}{color:blue;}.class{color:red;}", p.ToString(CssRestitution.OriginalWhenErrorOrInvalid));
  
  // === get details about error ===
  Assert.AreEqual(ErrorCode.UnexpectedToken, p.Errors[0].Code); // which type of error is it ?
  Assert.AreEqual(13, p.Errors[0].At); // where is my error ?
  Assert.AreEqual("{", p.Errors[0].Details); // which is the "unexpected token" ?
```

### Building CSS sheets programmatically

```C#
  var sheet = new Stylesheet
  {
      Rules =
      {
          new ClassRule
          {
              Selector = new SimpleSelector(".classname"),
              Properties =
              {
                  new Property { Name = "color", Values = { new CssSimpleValue("#fff")} },
                  new Property { Name = "background-image", Values = { new CssSimpleValue("url","test.png")} },
              }
          },
          new DirectiveRule
          {
              Selector = new DirectiveSelector{Name = "media",Arguments = "(max-width: 600px)",},
              ChildRules =
              {
                  new ClassRule
                  {
                      Selector = new SimpleSelector(".mediaclass"),
                      Properties = { new Property{Name = "opacity", HasStar = true, Values = {new CssSimpleValue("0.5")}}}
                  }
              }
          }
      }
  };

  Assert.That(sheet.IsValid);
  Assert.AreEqual(@".classname{color:#fff;background-image:url(test.png);}@media (max-width: 600px){.mediaclass{*opacity:0.5;}}", sheet.ToString());
```

# Links

Other .Net parser libraries:

- [CsCss](https://github.com/Athari/CsCss) Based on Mozilla Firefox parser. You wont get better if you're looking for a parser, but it does not support modification
- [ExCss](https://github.com/TylerBrinks/ExCSS) Another library, but it stops parsing silently in some cases, and fails on invalid CSS (That's why I developped NCss).

# Donate

My bitcoin address 
**146j s4Uj 9Dq9 tuik cSpZ Frbf yriM pbWE sg**
or via [the qrcode here](https://www.budyget.com/images/wallet.png)
