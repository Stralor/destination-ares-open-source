using UnityEngine;
using System.Collections;
using System.Runtime.Serialization;
using System.Reflection;

public sealed class VersionDeserializationBinder : SerializationBinder { 
	public override System.Type BindToType( string assemblyName, string typeName )
	{ 
		if ( !string.IsNullOrEmpty( assemblyName ) && !string.IsNullOrEmpty( typeName ) ) 
		{ 
			System.Type typeToDeserialize = null; 

			assemblyName = Assembly.GetExecutingAssembly().FullName; 
			
			// The following line of code returns the type. 
			typeToDeserialize = System.Type.GetType( System.String.Format( "{0}, {1}", typeName, assemblyName ) ); 
			
			return typeToDeserialize; 
		} 
		
		return null; 
	} 
}

