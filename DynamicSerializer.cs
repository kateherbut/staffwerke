public interface IDynamicSerializer 
{ 
    dynamic Deserialize(string payload); 
} 
 
public class DynamicSerializer : IDynamicSerializer 
{ 
    public dynamic Deserialize(string payload) 
    { 
        if (string.IsNullOrEmpty(payload)) 
        { 
            return new ExpandoObject(); 
        } 

        var parsedPayload = JObject.Parse(payload); 
        var result = new ExpandoObject(); 
        var expando = result as IDictionary<string, object>; 
        foreach (var (key, value) in parsedPayload) 
        { 
            this.TransformJtoken(value, key, expando); 
        } 

        return result; 
    } 

    private dynamic TransformJtoken(JToken value, string key, IDictionary<string, object> expando) 
    { 
        if (value is JValue jvalue) 
        { 
            expando[key] = jvalue.Value; 
            return expando[key]; 
        } 

        expando[key] = new ExpandoObject(); 

        // Nested object 
        if (value is JObject nestedObject) 
        { 
            foreach (var (nestedKey, nestedValue) in nestedObject) 
            { 
                this.TransformJtoken(nestedValue, nestedKey, expando[key] as IDictionary<string, object>); 
            } 

            return expando[key]; 
        } 

        // Array 
        if (value is JArray array) 
        { 
            expando[key] = array 
                .Select(x => this.TransformJtoken(x, key, expando[key] as IDictionary<string, object>)) 
                .ToList(); 

            return expando[key]; 
        } 

        return null; 
    } 
}