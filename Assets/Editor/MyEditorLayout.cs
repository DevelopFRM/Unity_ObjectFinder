using UnityEngine;
using UnityEditor;
using System.Collections;

namespace MyEditorUtil{

	// エディタレイアウトメソッドを少し使いやすいよう調整.
	public static class MyEditorLayout{


		//--------------------------------------------------------------------------------------------------
		// 引数の数だけスペースを入れる.
		public static void space( int num ){
			for ( int i = 0; i < num; ++i ){
				EditorGUILayout.Space();
			}
		}


		//--------------------------------------------------------------------------------------------------
		// 区切り線を入れる.
		public static void separatorLine( float width ){
			GUILayout.Box( "", GUILayout.Width( ( width < 0 ? 0 : width ) ), GUILayout.Height( 1 ) );
		}




	}


}





