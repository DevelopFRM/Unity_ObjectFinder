using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;


using MyEditorUtil;


// オブジェクト検索機能.
public class ObjectFinder : EditorWindow{


	//--- 定数 ---

	// 検索タイプ.
	public enum SearchType{
		Name = 0,	// オブジェクト名.
		Component,	// コンポーネント名.
	}

	// 検索範囲.
	public enum SearchScope{
		Hierarchy = 0,	// ヒエラルキー.
		ProjectWindow,	// プロジェクトウィンドウ.
		BothSide,		// 両方.
	}


	const float MAIN_LEFT_ELEM_WIDTH = 250.0f;				// メインウィンドウ左側描画領域幅.
	const float MAIN_MIDDLE_ELEM_WIDTH = 250.0f;			// メインウィンドウ中央描画領域幅.
	const float MAIN_RIGHT_ELEM_WIDTH = 300.0f;				// メインウィンドウ右側描画領域幅.
	const string IN_SCENE_EXTENSION = ".unity";				// シーンに配置されているゲームオブジェクトの拡張子.
	const string IN_PROJECTWINDOW_EXTENSION = ".prefab";	// プロジェクトウィンドウに配置されているゲームオブジェクトの拡張子.


	//--- 変数&プロパティ ---
	private SearchType searchType = SearchType.Name;			// 検索タイプ.
	private SearchScope searchScope = SearchScope.Hierarchy;	// 検索範囲.
	private ObjectTreeDrawSetting objTreeSetting = null;		// オブジェクトツリー描画領域セッティング.
	private ObjectTree objTree;						// オブジェクトツリー.
	private Vector2 middleScrPos = Vector2.zero;	// 中央描画領域のスクロール座標.
	private Vector2 rightScrPos = Vector2.zero;		// 右側描画領域のスクロール座標.
	private string searchText = string.Empty;		// 検索する文字列( オブジェクト名,コンポーネント名併用 ).
	private List< Object > objList = null;			// 検索結果オブジェクトリスト.
	private bool dumpFoldout = false;				// パラメータダンプテキスト表示するか否か.
	private bool wholeWord = false;					// 完全一致か否か.
	private bool caseSencitive = false;				// 大文字小文字.



	//--------------------------------------------------------------------------------------------------
	// window表示.
	[MenuItem( "Sample/ObjectFinder" )]
	public static void ShowWindow(){
		EditorWindow window = EditorWindow.GetWindow( typeof( ObjectFinder ) );
		window.minSize = new Vector2( MAIN_LEFT_ELEM_WIDTH + MAIN_MIDDLE_ELEM_WIDTH + MAIN_RIGHT_ELEM_WIDTH, 250 );
		window.position = new Rect( 200, 200, window.minSize.x, window.minSize.y );
	}



	//--------------------------------------------------------------------------------------------------
	// GUI描画.
	void OnGUI(){

		EditorGUILayout.BeginHorizontal();

		this.drawMainLeftElem();
		this.drawMainMiddleElem();
		this.drawMainRightElem();

		EditorGUILayout.EndHorizontal();
	
	}



	//--------------------------------------------------------------------------------------------------
	// メインウィンドウ左側描画領域.
	// 検索条件などを指定する領域.
	private void drawMainLeftElem(){
		EditorGUILayout.BeginVertical( GUI.skin.box, GUILayout.Width( MAIN_LEFT_ELEM_WIDTH ) );

		this.searchType = ( SearchType )EditorGUILayout.EnumPopup( "検索タイプ", this.searchType );
		this.searchScope = ( SearchScope )EditorGUILayout.EnumPopup( "検索範囲", this.searchScope );
		this.wholeWord = EditorGUILayout.Toggle( "完全一致か否か.", this.wholeWord );
		if ( !this.wholeWord )
			this.caseSencitive = EditorGUILayout.Toggle( "大小文字一致比較.", this.caseSencitive );


		switch ( this.searchType ){
			case SearchType.Name: EditorGUILayout.LabelField( "Input ObjectName" );	break;
			case SearchType.Component: EditorGUILayout.LabelField( "Input ComponentName" ); break;
			default: break;
		}
		this.searchText = EditorGUILayout.TextField( this.searchText, GUI.skin.textField );
			

		if ( GUILayout.Button( "Search" ) ){
			Debug.Log( "捜索します." );
			this.search();
		}


		MyEditorLayout.space( 3 );
		MyEditorLayout.separatorLine( MAIN_LEFT_ELEM_WIDTH );
		this.dumpFoldout = EditorGUILayout.Foldout( this.dumpFoldout, "Show Dump Palameter" );
		if ( this.dumpFoldout )
			this.drawDumpPalameter();

		EditorGUILayout.EndVertical();
	}



	//--------------------------------------------------------------------------------------------------
	// メインウィンドウ中央描画領域.
	private void drawMainMiddleElem(){
		EditorGUILayout.BeginVertical( GUI.skin.box, GUILayout.Width( MAIN_MIDDLE_ELEM_WIDTH ) );

		EditorGUILayout.LabelField( "検索結果表示欄." );
		if ( this.objList != null ){

			// ここで検索結果のオブジェクトをリスト表示する.
			this.middleScrPos = EditorGUILayout.BeginScrollView( this.middleScrPos );
			{

				for ( int i = 0; i < this.objList.Count; ++i ){
					if ( GUILayout.Button( this.objList[ i ].name ) ){

						this.objTree = null;
						this.objTree = new ObjectTree( ( GameObject )this.objList[ i ] );
					}
				}

			}
			EditorGUILayout.EndScrollView();
		}

		EditorGUILayout.EndVertical();
	}



	//--------------------------------------------------------------------------------------------------
	// メインウィンドウ右側描画領域.
	// 選択されたオブジェクトのツリー描画機能.
	private void drawMainRightElem(){
		EditorGUILayout.BeginVertical( GUI.skin.box, GUILayout.Width( MAIN_RIGHT_ELEM_WIDTH ) );

		EditorGUILayout.LabelField( "ObjectTreeWindow" );
		if ( this.objTreeSetting == null )
			this.objTreeSetting = new ObjectTreeDrawSetting();
		this.objTreeSetting.FaldOut = EditorGUILayout.Foldout( this.objTreeSetting.FaldOut, "Setting" );
		if ( this.objTreeSetting.FaldOut ){
			this.objTreeSetting.FontSize = EditorGUILayout.IntSlider( "FontSize", this.objTreeSetting.FontSize, 0, 20 );
		}

		if (	this.objList != null &&
				this.objTree != null ){

			this.rightScrPos = EditorGUILayout.BeginScrollView( this.rightScrPos );
			{

				if ( this.objTree.TreeExist ){
					for ( int i = 0; i < this.objTree.Tree.Length; ++i ){

						this.objTreeSetting.Style.normal.textColor = this.objTree.getTextColor( i );
						if ( GUILayout.Button( this.objTree.getElemText( i ), this.objTreeSetting.Style ) ){

						}
					}

				}else{
					this.objTreeSetting.Style.normal.textColor = this.objTree.getTextColor( 0 );
					if ( GUILayout.Button( this.objTree.getElemText( 0 ), this.objTreeSetting.Style ) ){

					}
				}
			}
			EditorGUILayout.EndScrollView();
		}

		EditorGUILayout.EndVertical();
	}



	//--------------------------------------------------------------------------------------------------
	// 検索設定のパラメータ表示.
	private void drawDumpPalameter(){

		EditorGUILayout.LabelField( "Scope : " + this.searchScope.ToString() );
		EditorGUILayout.LabelField( "Search To : " + this.searchType.ToString() );
		EditorGUILayout.LabelField( "TargetName : " + this.searchText );

	}



	//--------------------------------------------------------------------------------------------------
	// オブジェクト検索.
	private void search(){

		if ( this.objList != null )
			this.objList.Clear();
		else
			this.objList = new List< Object >();

		Object[] allObject = Resources.FindObjectsOfTypeAll( typeof( GameObject ) );
		if ( this.searchType == SearchType.Name ){
			
			foreach ( Object anObject in allObject ){
				if ( this.samplingSearchScope( anObject ) != null ){
					this.objList.Add( anObject );
				}
			}

		}else{

			foreach ( GameObject anObject in allObject ){
				if ( 	anObject.GetComponent( this.searchText ) != null &&
						this.samplingSearchScope( anObject ) != null ){
					this.objList.Add( anObject );
				}
			}
		}


	}



	//--------------------------------------------------------------------------------------------------
	// 検索範囲及びテキストで抽出.
	private Object samplingSearchScope( Object obj ){

		// テキストが適合しない場合はここでnullを返す.
		if ( this.wholeWord ){
			if ( !obj.name.Equals( this.searchText ) )
				return null;
		}else{

			// 含まれている文字列で大文字小文字一致比較.
			if ( this.caseSencitive ){

				if ( obj.name.IndexOf( this.searchText ) < 0 )
					return null;

			}else{
				string lowerName = obj.name.ToLower();
				if ( lowerName.IndexOf( this.searchText.ToLower() ) < 0 )
					return  null;
			}
		}

		string path = AssetDatabase.GetAssetOrScenePath( obj );
		if ( ( 0 <= path.IndexOf( IN_SCENE_EXTENSION ) ) && ( this.searchScope == SearchScope.Hierarchy ) ){
			return obj;
		}else if ( ( 0 <= path.IndexOf( IN_PROJECTWINDOW_EXTENSION ) ) && ( this.searchScope == SearchScope.ProjectWindow ) ){
			return obj;
		}else if ( this.searchScope == SearchScope.BothSide ){
			return obj;
		}
		return null;
	}



	//--- 内包クラス宣言 ---



	//==================================================================================================
	// オブジェクトツリー描画領域セッティング.
	private class ObjectTreeDrawSetting{

		// 表示の可否.
		private bool faldOut = false;
		public bool FaldOut{ get{ return this.faldOut; } set{ this.faldOut = value; } }

		// ボタンテキストフォントサイズ.
		private int fontSize = 10;
		public int FontSize{
			get{ return this.fontSize; } 
			set{ this.fontSize = value;
				this.style.fontSize = this.fontSize;
			}
		}

		// ボタンテキストスタイル.
		private GUIStyle style = null;
		public GUIStyle Style{ get{ return this.style; } }


		//--------------------------------------------------------------------------------------------------
		// コンストラクタ.
		public ObjectTreeDrawSetting(){
			this.style = new GUIStyle( GUI.skin.button );
			this.style.fontSize = this.fontSize;
			this.style.alignment = TextAnchor.MiddleLeft;
		}


	}



	//==================================================================================================
	// オブジェクトツリー.
	public class ObjectTree{

		// オブジェクトツリールート.
		private ObjectTreeElem root;
		public ObjectTreeElem Root{
			get{ return this.root; }
		}

		// オブジェクトツリー.
		private ObjectTreeElem[] tree = null;
		public ObjectTreeElem[] Tree { 
			get{ 
				return this.tree;
			}
		}

		// ツリーが存在するか否か.
		public bool TreeExist{
			get{ return this.tree != null; }
		}

		private GameObject selectedObj;// 選択されたオブジェクト.


		//--------------------------------------------------------------------------------------------------
		// コンストラクタ.
		public ObjectTree( GameObject obj ){
			this.selectedObj = obj;
			this.root = new ObjectTreeElem( 0, obj.transform.root.gameObject );
			this.tree = this.createTree();
		}


		//--------------------------------------------------------------------------------------------------
		// ツリー要素のテキスト取得.
		public string getElemText( int index ){

			if ( !this.TreeExist )
				return this.root.ButtonText;

			if ( index < 0 || this.tree.Length < index )
				return "index error";
			return this.tree[ index ].ButtonText;
		}


		//--------------------------------------------------------------------------------------------------
		// ツリー要素のテキストカラー取得.
		public Color getTextColor( int index ){

			if ( !this.TreeExist )
				return ( this.root.Obj.GetInstanceID() == this.selectedObj.GetInstanceID() ) ? Color.cyan : Color.black;
			else
				return ( this.tree[ index ].Obj.GetInstanceID() == this.selectedObj.GetInstanceID() ) ? Color.cyan : Color.black;
		}


		//--------------------------------------------------------------------------------------------------
		// オブジェクトツリーの取得.
		private ObjectTreeElem[] createTree(){
			return this.root.getBranch();
		}

	}



	//==================================================================================================
	// オブジェクトツリー各要素.
	public class ObjectTreeElem{


		// 前の要素.
		private ObjectTreeElem prev = null;
		public ObjectTreeElem Prev{ get{ return this.prev; } }

		// 次の要素群.
		private ObjectTreeElem[] next = null;
		public ObjectTreeElem[] Next{ get{ return this.next; } }

		// ルートからの深さ.
		private int count;
		public int Count{ get{ return this.count; } }

		// 自身の保持するオブジェクト.
		private GameObject obj;	
		public GameObject Obj{ get{ return this.obj; } }

		// ボタンに表示するテキスト.
		private string buttonText = string.Empty;
		public string ButtonText{ get{ return this.buttonText; } }



		//--------------------------------------------------------------------------------------------------
		// コンストラクタ.
		public ObjectTreeElem( int count, GameObject obj ){
			this.count = count;
			this.obj = obj;

			string depth = "    ";
			for ( int i = 0; i < this.count; ++i ){
				this.buttonText += depth;
			}
			this.buttonText += ( 0 < this.count ) ? ">" : "";
			this.buttonText += this.obj.name;

			if ( this.obj.transform.childCount != 0 ){
				this.next = new ObjectTreeElem[ this.obj.transform.childCount ];
				for ( int i = 0; i < this.obj.transform.childCount; ++i ){
					this.next[ i ] = new ObjectTreeElem( this.count + 1, this.obj.transform.GetChild( i ).gameObject );
				}
			}

		}


		//--------------------------------------------------------------------------------------------------
		// 自身より下の階層にいるオブジェクトを配列化して取得.
		public ObjectTreeElem[] getBranch(){

			if ( this.obj.transform.childCount < 1 ){
				return null;
			}else{

				List< ObjectTreeElem > ret = new List< ObjectTreeElem >();
				ret.Add( this );
				for ( int i = 0; i < this.obj.transform.childCount; ++i ){

					ObjectTreeElem[] elem = this.next[ i ].getBranch();
					if ( elem != null )
						ret.AddRange( this.next[ i ].getBranch() );
					else
						ret.Add( this.next[ i ] );
				}
				return ret.ToArray();
			}
		}



	}



}












