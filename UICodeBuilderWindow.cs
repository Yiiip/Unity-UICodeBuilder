/**
 * Created by YingpengLiu.
 *
 * Date: 2019-01-07
 * Description: UI代码结构自动生成工具
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Rotorz.Games.Collections;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LYP.UICodeBuilder
{
	public class UICodeBuilderWindow : EditorWindow
	{
		private const string MENU_TITLE = "UI Code Builder";
		private const string WIN_TITLE = "UI Builder";
		private static string iconPath = "Assets/Editor/UI/UICodeBuilder/icon.png";
		private static string[] arrSuperclassNames = new string[]
		{
			"MonoBehaviour",
			"BaseView",
			"NONE"
		};
		private static string[] arrInterfaceNames = new string[]
		{
			"ViewHolder"
		};
		private static string[] arrVarAccessNames = new string[]
		{
			"public",
			"private"
		};
		private static UICodeBuilderWindow mWindow = null;
		private SerializedObject serializedObj;

		private float halfViewWidth;
		private Vector2 scrollWidgetPos;
		private Vector2 scrollCustomObjPos;
		private Vector2 scrollTextPos;
		private int selectedTabIndex = 0;
		private int selectedPopUpSuperclassIndex = (int) UICodeBuilderConfig.SuperclassIndex.MonoBehaviour;
		private int selectedPopUpVarAccessIndex = 0;

		private StringBuilder codeStateText;
		private StringBuilder codeStructureText;
		private StringBuilder codeEventText;
		private StringBuilder codeAssignText;
		private StringBuilder codeAllText;

		//选择的UI根节点
		private GameObject root;
		//UI控件列表
		private List<UIBehaviour> uiWidgets = new List<UIBehaviour>();
		//UI自定义对象列表
		private List<GameObject> uiObjects = new List<GameObject>();

		//需要注册事件的控件,可通过toggle选择
		private Dictionary<string, bool> toogleDicEventWidgets = new Dictionary<string, bool>();
		//需要实现的接口
		private Dictionary<string, bool> toogleDicInterfaces = new Dictionary<string, bool>();

		//变量编号
		private int variableNum;
		//缓存所有变量名和对应控件对象，对重名作处理
		private Dictionary<string, object> variableNameDic = new Dictionary<string, object>();
		//保存的类名
		private string className;
		//脚本组件类型
		private Type scriptType;

		private string regionStartFmt { get { return selectedTabIndex == 0 ? UICodeBuilderConfig.regionStartFmt : ""; } }
		private string regionEnd { get { return selectedTabIndex == 0 ? UICodeBuilderConfig.regionEnd : ""; } }
		private string statementRegion { get { return UICodeBuilderConfig.statementRegion; } }
		private string eventRegion { get { return selectedTabIndex == 0 ? UICodeBuilderConfig.eventRegion : ""; } }
		private string assignRegion { get { return selectedTabIndex == 0 ? UICodeBuilderConfig.assignRegion : ""; } }
		private string methodStartFmt { get { return selectedTabIndex == 0 ? UICodeBuilderConfig.methodStartFmt : ""; } }
		private string methodEnd { get { return selectedTabIndex == 0 ? UICodeBuilderConfig.methodEnd : ""; } }
		private string assignCodeFmt { get { return selectedTabIndex == 0 ? UICodeBuilderConfig.assignCodeFmt : ""; } }
		private string assignGameObjectCodeFmt { get { return selectedTabIndex == 0 ? UICodeBuilderConfig.assignGameObjectCodeFmt : ""; } }
		private string assignRootCodeFmt { get { return selectedTabIndex == 0 ? UICodeBuilderConfig.assignRootCodeFmt : ""; } }
		private string onClickSerilCode { get { return selectedTabIndex == 0 ? UICodeBuilderConfig.onClickSerilCode : ""; } }
		private string onClickSerilCodeForBaseView { get { return selectedTabIndex == 0 ? UICodeBuilderConfig.onClickSerilCodeForBaseView : ""; } }
		private string onValueChangeSerilCode { get { return selectedTabIndex == 0 ? UICodeBuilderConfig.onValueChangeSerilCode : ""; } }
		private string btnCallbackSerilCode { get { return selectedTabIndex == 0 ? UICodeBuilderConfig.btnCallbackSerilCode : ""; } }
		private string eventCallbackSerilCode { get { return selectedTabIndex == 0 ? UICodeBuilderConfig.eventCallbackSerilCode : ""; } }
		private string onClickedCallbackSerilCode { get { return selectedTabIndex == 0 ? UICodeBuilderConfig.onClickedCallbackSerilCode : ""; } }

		//------------------------------------------------------------------------------------------
		[MenuItem("Tools/UI/" + MENU_TITLE)]
		private static void ShowWindow()
		{
			if (mWindow == null)
			{
				mWindow = GetWindow<UICodeBuilderWindow>();
			}

			Texture2D icon = EditorGUIUtility.Load(iconPath) as Texture2D;
			mWindow.titleContent = new GUIContent(WIN_TITLE);
			mWindow.titleContent.image = icon;
			mWindow.Show();
		}

		void OnEnable()
		{
			serializedObj = new SerializedObject(this);
		}

		void OnGUI()
		{
			serializedObj.Update();

			if (mWindow == null)
			{
				mWindow = GetWindow<UICodeBuilderWindow>();
			}
			halfViewWidth = EditorGUIUtility.currentViewWidth / 2f;
			// halfViewHeight = mWindow.position.height / 2f;

			using(new EditorGUILayout.HorizontalScope())
			{
				//左半部分
				using(EditorGUILayout.VerticalScope vScope = new EditorGUILayout.VerticalScope(GUILayout.Width(halfViewWidth)))
				{
					GUI.backgroundColor = Color.white;
					Rect rect = vScope.rect;
					rect.height = mWindow.position.height;
					GUI.Box(rect, "");

					DrawSelectUI();
					DrawFindWidget();
					DrawWidgetList();
					DrawCustomObjectList();
				}
				//右半部分
				using(new EditorGUILayout.VerticalScope(GUILayout.Width(halfViewWidth)))
				{
					DrawCodeGenerateTitle();
					DrawCodeGenerateTabs();
				}
			}

			serializedObj.ApplyModifiedProperties();
		}

		private void DrawSelectUI()
		{
			EditorGUILayout.Space();
			using(EditorGUILayout.HorizontalScope hScope = new EditorGUILayout.HorizontalScope())
			{
				GUI.backgroundColor = Color.white;
				Rect rect = hScope.rect;
				rect.height = EditorGUIUtility.singleLineHeight;
				GUI.Box(rect, "");

				EditorGUILayout.LabelField("Selected UI Node:", GUILayout.Width(halfViewWidth / 3f));
				GameObject lastRoot = root;
				root = EditorGUILayout.ObjectField(root, typeof(GameObject), true) as GameObject;

				if (lastRoot != null && lastRoot != root)
				{
					uiWidgets.Clear();
					uiObjects.Clear();
				}
			}
		}

		private void DrawFindWidget()
		{
			EditorGUILayout.Space();
			using(EditorGUILayout.HorizontalScope hScope = new EditorGUILayout.HorizontalScope())
			{
				GUI.backgroundColor = Color.white;
				Rect rect = hScope.rect;
				rect.height = EditorGUIUtility.singleLineHeight;
				GUI.Box(rect, "");

				if (GUILayout.Button("SCAN UI Node", GUILayout.Width(halfViewWidth / 2f)))
				{
					ScanWidgets();
				}

				if (GUILayout.Button("Clear Widgets"))
				{
					uiWidgets.Clear();
					toogleDicEventWidgets.Clear();
				}
				if (GUILayout.Button("Clear Customized"))
				{
					uiObjects.Clear();
					toogleDicEventWidgets.Clear();
				}
			}
		}

		private void DrawWidgetList()
		{
			EditorGUILayout.Space();

			ReorderableListGUI.Title("UI Widgets");
			scrollWidgetPos = EditorGUILayout.BeginScrollView(scrollWidgetPos);
			ReorderableListGUI.ListField<UIBehaviour>(uiWidgets, DrawWidget);
			EditorGUILayout.EndScrollView();
		}

		private UIBehaviour DrawWidget(Rect position, UIBehaviour item)
		{
			item = (UIBehaviour) EditorGUI.ObjectField(position, item, typeof(UIBehaviour), true);
			return item;
		}

		private void DrawCustomObjectList()
		{
			EditorGUILayout.Space();

			ReorderableListGUI.Title("Customized UI Objects");
			scrollCustomObjPos = EditorGUILayout.BeginScrollView(scrollCustomObjPos);
			ReorderableListGUI.ListField<GameObject>(uiObjects, DrawCustomObject);
			EditorGUILayout.EndScrollView();
		}

		private GameObject DrawCustomObject(Rect position, GameObject item)
		{
			item = (GameObject) EditorGUI.ObjectField(position, item, typeof(GameObject), true);
			return item;
		}

		private void DrawCodeGenerateTitle()
		{
			EditorGUILayout.Space();
			using(var hScope = new EditorGUILayout.HorizontalScope(GUILayout.Height(EditorGUIUtility.singleLineHeight)))
			{
				GUI.backgroundColor = Color.white;
				Rect rect = hScope.rect;
				GUI.Box(rect, "");

				EditorGUILayout.LabelField("UI CODE BUILDER");
			}
		}

		private void DrawCodeGenerateTabs()
		{
			EditorGUILayout.Space();

			selectedTabIndex = GUILayout.Toolbar(selectedTabIndex, new string[] { "C#" });

			switch (selectedTabIndex)
			{
				case 0:
					DrawCSPage();
					break;
				default:
					break;
			}
		}

		private void DrawCSPage()
		{
			EditorGUILayout.Space();
			using(EditorGUILayout.HorizontalScope hScope = new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Label("Superclass:", GUILayout.Width(90f));
				selectedPopUpSuperclassIndex = EditorGUILayout.Popup(selectedPopUpSuperclassIndex, arrSuperclassNames);
			}
			EditorGUILayout.Space();
			using(EditorGUILayout.HorizontalScope hScope = new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Label("Interface:", GUILayout.Width(90f));
				foreach (string name in arrInterfaceNames)
				{
					if (!toogleDicInterfaces.ContainsKey(name))
					{
						toogleDicInterfaces.Add(name, false);
					}
				}
				foreach (string name in arrInterfaceNames)
				{
					toogleDicInterfaces[name] = EditorGUILayout.ToggleLeft(name, toogleDicInterfaces[name], GUILayout.Width(halfViewWidth / arrInterfaceNames.Length));
				}
			}

			EditorGUILayout.Space();
			using(EditorGUILayout.HorizontalScope hScope = new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button("1. Declare Variables", GUILayout.Width(halfViewWidth / 3f)))
				{
					BuildStatementCode();
				}
				selectedPopUpVarAccessIndex = EditorGUILayout.Popup(selectedPopUpVarAccessIndex, arrVarAccessNames);
			}

			EditorGUILayout.Space();
			using(EditorGUILayout.VerticalScope vScope = new EditorGUILayout.VerticalScope())
			{
				GUI.backgroundColor = Color.white;
				GUI.Box(vScope.rect, "");

				EditorGUILayout.LabelField("Select widgets to add events:");
				DrawEventWidget();

				EditorGUILayout.Space();
				if (GUILayout.Button("2. Add Events", GUILayout.Width(halfViewWidth / 3f)))
				{
					BuildEventsCode();
				}
			}

			EditorGUILayout.Space();
			using(EditorGUILayout.HorizontalScope hScope = new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button("3. Find View By Name", GUILayout.Width(halfViewWidth / 2f)))
				{
					BuildAssignmentCode();
				}
				if (GUILayout.Button("4. CREATE & SAVE Script", GUILayout.Width(halfViewWidth / 2f)))
				{
					CreateCsUIScript();
				}
			}

			EditorGUILayout.Space();
			using(EditorGUILayout.HorizontalScope hScope = new EditorGUILayout.HorizontalScope())
			{
				if (selectedPopUpSuperclassIndex != (int) UICodeBuilderConfig.SuperclassIndex.NONE)
				{
					if (GUILayout.Button("5. Add Script Component", GUILayout.Width(halfViewWidth / 2f)))
					{
						AddScriptComponent();
					}
					if (GUILayout.Button("6. Bind Refrences", GUILayout.Width(halfViewWidth / 2f)))
					{
						BindSerializeWidgetRefrences();
					}
				}
			}

			DrawPreviewText();
		}

		private void DrawEventWidget()
		{
			using(EditorGUILayout.HorizontalScope hScope = new EditorGUILayout.HorizontalScope())
			{
				foreach (var elem in System.Enum.GetValues(typeof(UICodeBuilderConfig.EventWidgetType)))
				{
					for (int i = 0; i < uiWidgets.Count; i++)
					{
						if (uiWidgets[i] == null)
						{
							continue;
						}

						Type type = uiWidgets[i].GetType();
						if (type == null)
						{
							Debug.LogWarning("Type error! " + uiWidgets[i].name);
							continue;
						}

						if (type.Name == elem.ToString() && !toogleDicEventWidgets.ContainsKey(type.Name))
						{
							toogleDicEventWidgets.Add(type.Name, true);
						}
					}
				}

				//移出控件不存在的事件类型
				List<string> notExist = new List<string>();
				foreach (string tName in toogleDicEventWidgets.Keys)
				{
					bool exist = false;
					for (int i = 0; i < uiWidgets.Count; i++)
					{
						Type type = uiWidgets[i].GetType();
						if (type != null && type.Name == tName)
						{
							exist = true;
							break;
						}
					}
					if (!exist)
					{
						notExist.Add(tName);
					}
				}
				foreach (string name in notExist)
				{
					toogleDicEventWidgets.Remove(name);
				}
				notExist.Clear();

				//绘制toggle,注意不能遍历dic的同时赋值
				List<string> list = new List<string>(toogleDicEventWidgets.Keys);
				foreach (string name in list)
				{
					toogleDicEventWidgets[name] = EditorGUILayout.ToggleLeft(name, toogleDicEventWidgets[name], GUILayout.Width(halfViewWidth / 7f));
				}
			}
		}

		private void DrawPreviewText()
		{
			EditorGUILayout.Space();

			if (GUILayout.Button("Copy Code"))
			{
				TextEditor p = new TextEditor();
				if (codeStateText != null)
				{
					codeAllText = new StringBuilder(codeStateText.ToString());
					codeAllText.Append(codeAssignText);
					codeAllText.Append(codeEventText);
					p.text = codeAllText.ToString();
					p.OnFocus();
					p.Copy();

					EditorUtility.DisplayDialog("Tips", "Copy succeed!", "OK");
				}
				else
				{
					EditorUtility.DisplayDialog("Tips", "Copy nothing! Please check 1st step.", "OK");
				}
			}

			using(var ver = new EditorGUILayout.VerticalScope())
			{
				GUI.backgroundColor = new Color(43f/255f, 43f/255f, 43f/255f);
				GUI.Box(ver.rect, "");

				EditorGUILayout.HelpBox("Code Preview:", MessageType.None);

				using(var scr = new EditorGUILayout.ScrollViewScope(scrollTextPos))
				{
					scrollTextPos = scr.scrollPosition;

					GUIStyle cssNormalText = new GUIStyle();
					cssNormalText.normal.textColor = new Color(240f/255f, 240f/255f, 240f/255f);

					GUIStyle cssStructureText = new GUIStyle();
					cssStructureText.normal.textColor = new Color(70f/255f, 149f/255f, 214f/255f);

					GUILayout.Label(GetNamespaceString(), cssNormalText);
					if (selectedPopUpSuperclassIndex == (int) UICodeBuilderConfig.SuperclassIndex.BaseView)
					{
						GUILayout.Label(string.Format(UICodeBuilderConfig.baseviewContextClass, "YourClazz"), cssStructureText);
					}
					GUILayout.Label(GetClazzString("YourClazz", GetInterfacesString()), cssNormalText);

					if (codeStateText != null && !string.IsNullOrEmpty(codeStateText.ToString()) && selectedTabIndex == 0)
					{
						GUIStyle cssStateText = new GUIStyle();
						cssStateText.normal.textColor = new Color(254f/255f, 217f/255f, 92f/255f);
						GUILayout.Label(codeStateText.ToString(), cssStateText);
					}

					if (codeStructureText != null && !string.IsNullOrEmpty(codeStructureText.ToString()))
					{
						GUILayout.Label(codeStructureText.ToString(), cssStructureText);
					}

					if (codeAssignText != null && !string.IsNullOrEmpty(codeAssignText.ToString()))
					{
						GUIStyle cssAssignText = new GUIStyle();
						cssAssignText.normal.textColor = new Color(40f/255f, 204f/255f, 117f/255f);
						GUILayout.Label(codeAssignText.ToString(), cssAssignText);
					}

					if (codeEventText != null && !string.IsNullOrEmpty(codeEventText.ToString()))
					{
						GUIStyle cssEventText = new GUIStyle();
						cssEventText.normal.textColor = new Color(250f/255f, 110f/255f, 87f/255f);
						GUILayout.Label(codeEventText.ToString(), cssEventText);
					}

					GUILayout.Label(UICodeBuilderConfig.classEnd, cssNormalText);
				}
			}
		}

		private bool IsMonoBased()
		{
			return (selectedPopUpSuperclassIndex == (int) UICodeBuilderConfig.SuperclassIndex.MonoBehaviour) ||
				(selectedPopUpSuperclassIndex == (int) UICodeBuilderConfig.SuperclassIndex.BaseView);
		}

		private bool IsPublic()
		{
			return (arrVarAccessNames[selectedPopUpVarAccessIndex] == "public");
		}

		private bool IsToogledViewHolder()
		{
			return toogleDicInterfaces[arrInterfaceNames[(int) UICodeBuilderConfig.InterfaceIndex.ViewHolder]];
		}

		private bool CheckSelectedRoot()
		{
			if (root == null)
			{
				EditorUtility.DisplayDialog("Tips", "Please select an UI node!", "OK");
				return false;
			}
			return true;
		}

		private bool CheckScriptSaved()
		{
			if (root == null || string.IsNullOrEmpty(className))
			{
				EditorUtility.DisplayDialog("Warning", "Please check step 1~4 firstly.", "OK");
				return false;
			}
			return true;
		}

		private bool CheckIsAddedScriptComponent()
		{
			if (scriptType == null)
			{
				EditorUtility.DisplayDialog("Warning", "Please check step 1~5 firstly.", "OK");
				return false;
			}
			return true;
		}

		private bool CheckEditorIsCompiling()
		{
			if (EditorApplication.isCompiling)
			{
				EditorUtility.DisplayDialog("Warning", "Please wait for Editor compiling.", "OK");
				return true;
			}
			return false;
		}

		private void ScanWidgets()
		{
			if (!CheckSelectedRoot())
			{
				return;
			}

			uiWidgets.Clear();
			RecursiveUI(root.transform, (tran) =>
			{
				UIBehaviour[] widgets = tran.GetComponents<UIBehaviour>();
				for (int i = 0; i < widgets.Length; i++)
				{
					UIBehaviour widget = widgets[i];
					if (widget != null && !ShouldFilterUIWidget(widget) && !uiWidgets.Contains(widget))
					{
						uiWidgets.Add(widget);
					}
				}
			});
		}

		private bool ShouldFilterUIWidget(UIBehaviour ui)
		{
			if (ui is Outline || ui is Shadow)
			{
				return true;
			}
			return false;
		}

		public void RecursiveUI(Transform parent, UnityAction<Transform> callback)
		{
			if (callback != null)
			{
				callback(parent);
			}

			for (int i = 0; i < parent.childCount; i++)
			{
				Transform child = parent.GetChild(i);
				this.RecursiveUI(child, callback);
			}
		}

		private string VarNameFilter(string varName)
		{
			string result = varName;
			result = result.Replace(" ", "");
			result = result.Replace("(", "");
			result = result.Replace(")", "");
			result = result.Replace(".", "");
			result = result.Replace(",", "");
			result = result.Replace("-", "");
			result = result.Replace("+", "");
			// Regex regNum = new Regex("^[0-9]");
			// if (regNum.IsMatch(result))
			// {
			// 	result = "ui" + result;
			// }
			return result;
		}

		private string BuildStatementCode()
		{
			variableNum = 0;
			variableNameDic.Clear();

			codeStateText = null;
			codeStateText = new StringBuilder();

			codeStateText.Append(UICodeBuilderConfig.statementRegion);
			//非mono类声明一个transform
			if (!IsMonoBased())
			{
				codeStateText.Append(UICodeBuilderConfig.stateTransform);
			}

			//控件列表
			for (int i = 0; i < uiWidgets.Count; i++)
			{
				if (uiWidgets[i] == null) continue;

				Type type = uiWidgets[i].GetType();
				if (type == null)
				{
					Debug.LogError("BuildUICode type error !");
					return "";
				}

				string typeName = type.Name;
				string prefixName = type.Name;
				string widgetName = uiWidgets[i].name;

				if (prefixName == "Image") { prefixName = "Img"; }
				else if (prefixName == "Button") { prefixName = "Btn"; }
				if (widgetName.StartsWith(prefixName) || widgetName.StartsWith(prefixName.ToLower()))
				{
					widgetName = widgetName.Remove(0, prefixName.Length);
				}

				string varName = string.Format("{0}_{1}", prefixName.ToLower(), widgetName);
				varName = VarNameFilter(varName);
				//重名处理
				if (variableNameDic.ContainsKey(varName))
				{
					++variableNum;
					varName += ("0" + variableNum);
				}
				variableNameDic.Add(varName, uiWidgets[i]);

				string varNameToShow = varName.Replace("_", "");
				if (IsMonoBased())
				{
					if (IsPublic())
					{
						codeStateText.AppendFormat(UICodeBuilderConfig.serilStateCodePublicFmt, typeName, varNameToShow);
					}
					else
					{
						codeStateText.AppendFormat(UICodeBuilderConfig.serilStateCodePrivateFmt, typeName, varNameToShow);
					}
				}
				else
				{
					if (IsPublic())
					{
						codeStateText.AppendFormat(UICodeBuilderConfig.stateCodePublicFmt, typeName, varNameToShow);
					}
					else
					{
						codeStateText.AppendFormat(UICodeBuilderConfig.stateCodePrivateFmt, typeName, varNameToShow);
					}
				}
			}
			//其他对象列表，目前都是GameObject
			for (int i = 0; i < uiObjects.Count; i++)
			{
				if (uiObjects[i] == null) continue;

				Type type = uiObjects[i].GetType();
				if (type == null)
				{
					Debug.LogError("BuildUICode type error !");
					return "";
				}

				string typeName = type.Name;
				string varName = string.Format("obj_{0}", uiObjects[i].name);
				varName = VarNameFilter(varName);
				//重名处理
				if (variableNameDic.ContainsKey(varName))
				{
					++variableNum;
					varName += ("0" + variableNum);
				}
				variableNameDic.Add(varName, uiObjects[i]);

				string varNameToShow = varName.Replace("_", "");
				if (IsMonoBased())
				{
					if (IsPublic())
					{
						codeStateText.AppendFormat(UICodeBuilderConfig.serilStateCodePublicFmt, typeName, varNameToShow);
					}
					else
					{
						codeStateText.AppendFormat(UICodeBuilderConfig.serilStateCodePrivateFmt, typeName, varNameToShow);
					}
				}
				else
				{
					if (IsPublic())
					{
						codeStateText.AppendFormat(UICodeBuilderConfig.stateCodePublicFmt, typeName, varNameToShow);
					}
					else
					{
						codeStateText.AppendFormat(UICodeBuilderConfig.stateCodePrivateFmt, typeName, varNameToShow);
					}
				}
			}

			codeStateText.Append(UICodeBuilderConfig.regionEnd);

			codeStructureText = null;
			codeStructureText = new StringBuilder();
			if (IsToogledViewHolder())
			{
				codeStructureText.Append(UICodeBuilderConfig.viewholderSetEmpty);
			}

			if (selectedPopUpSuperclassIndex == (int) UICodeBuilderConfig.SuperclassIndex.BaseView)
			{
				codeStructureText.Append(UICodeBuilderConfig.baseviewStart);
				codeStructureText.Append(UICodeBuilderConfig.baseviewOnEnter);
				codeStructureText.Append(UICodeBuilderConfig.baseviewOnExit);
				codeStructureText.Append(UICodeBuilderConfig.baseviewOnResume);
				codeStructureText.Append(UICodeBuilderConfig.baseviewOnPause);
			}

			// Debug.Log(codeStateText);
			// Debug.Log(codeInitialText);
			return codeStateText.ToString() + codeStructureText.ToString();
		}

		private string BuildEventsCode()
		{
			codeEventText = null;
			codeEventText = new StringBuilder();

			codeEventText.Append(eventRegion);
			codeEventText.AppendFormat(methodStartFmt, "InitEvents");

			var tempBtnNames = new List<string>();

			bool hasEventWidget = false; //标识是否有控件注册了事件，动态增加换行符
			for (int i = 0; i < uiWidgets.Count; i++)
			{
				if (uiWidgets[i] == null) continue;

				//剔除不是事件或者是事件但未勾选toggle的控件
				string typeName = uiWidgets[i].GetType().Name;
				if (!toogleDicEventWidgets.ContainsKey(typeName) || toogleDicEventWidgets[typeName] == false)
				{
					continue;
				}

				foreach (string vName in variableNameDic.Keys)
				{
					if (uiWidgets[i].Equals(variableNameDic[vName]))
					{
						if (!string.IsNullOrEmpty(vName))
						{
							string varName = vName;
							string varNameToShow = varName.Replace("_", "");
							string methodName = varName.Substring(varName.IndexOf('_') + 1);
							if (uiWidgets[i] is Button)
							{
								if (!tempBtnNames.Contains(varNameToShow))
								{
									tempBtnNames.Add(varNameToShow);
								}

								string onClickStr = "";
								if (selectedPopUpSuperclassIndex == (int) UICodeBuilderConfig.SuperclassIndex.BaseView)
								{
									onClickStr = string.Format(onClickSerilCodeForBaseView, varNameToShow);
								}
								else
								{
									onClickStr = string.Format(onClickSerilCode, varNameToShow, methodName);
								}

								if (hasEventWidget)
								{
									string str = codeEventText.ToString();
									codeEventText.Insert(str.LastIndexOf(';') + 1, "\n" + onClickStr);
								}
								else
								{
									codeEventText.Append(onClickStr);
								}

								if (selectedPopUpSuperclassIndex != (int) UICodeBuilderConfig.SuperclassIndex.BaseView)
								{
									codeEventText.AppendFormat(btnCallbackSerilCode, methodName);
								}
								hasEventWidget = true;
							}
							else
							{
								string addEventStr = string.Format(onValueChangeSerilCode, varNameToShow, methodName);
								if (hasEventWidget)
								{
									codeEventText.Insert(codeEventText.ToString().LastIndexOf(';') + 1, addEventStr);
								}
								else
								{
									codeEventText.Append(addEventStr);
								}

								string paramType = "";
								foreach (string widgetType in UICodeBuilderConfig.eventDelegateParamDic.Keys)
								{
									if (typeName == widgetType)
									{
										paramType = UICodeBuilderConfig.eventDelegateParamDic[widgetType];
										break;
									}
								}

								if (!string.IsNullOrEmpty(paramType))
								{
									codeEventText.AppendFormat(eventCallbackSerilCode, methodName, paramType);
								}

								hasEventWidget = true;
							}
						}
						break;
					}
				}
			}

			string codeStr = codeEventText.ToString();
			if (hasEventWidget)
			{
				codeEventText.Insert(codeStr.LastIndexOf(';') + 1, methodEnd);
			}
			else
			{
				codeEventText.Append(methodEnd);
			}

			//For BaseView.OnBtnClick(GameObject go)
			if (selectedPopUpSuperclassIndex == (int) UICodeBuilderConfig.SuperclassIndex.BaseView)
			{
				codeEventText.Append(onClickedCallbackSerilCode);
				for (int i = 0; i < tempBtnNames.Count; i++)
				{
					if (i == 0)
					{
						codeEventText.AppendFormat(UICodeBuilderConfig.ifGoStartFmt, tempBtnNames[i]);
					}
					else
					{
						codeEventText.AppendFormat(UICodeBuilderConfig.ifelseGoStartFmt, tempBtnNames[i]);
					}
					codeEventText.Append(UICodeBuilderConfig.ifEnd);
				}
				codeEventText.Append(methodEnd);
			}

			codeEventText.Append(regionEnd);
			return codeEventText.ToString();
		}

		private void BuildAssignmentCode()
		{
			if (!CheckSelectedRoot())
			{
				return;
			}

			codeAssignText = new StringBuilder();

			codeAssignText.Append(assignRegion);
			codeAssignText.AppendFormat(methodStartFmt, "InitViews");
			if (!IsMonoBased() && selectedTabIndex == 0)
			{
				codeAssignText.Append(UICodeBuilderConfig.assignTransform);
			}

			Dictionary<Transform, string> allPath = GetChildrenPaths(root);

			if (variableNameDic == null)
			{
				return;
			}

			//格式：变量名 = transform.Find("").Getcomponent<>();
			foreach (string vName in variableNameDic.Keys)
			{
				object obj = variableNameDic[vName];
				if (obj == null) continue;

				string varNameToShow = vName.Replace("_", "");

				string path = "";
				bool isRootComponent = false;
				foreach (Transform tran in allPath.Keys)
				{
					if (tran == null) continue;

					UIBehaviour behav = obj as UIBehaviour;
					if (behav != null)
					{
						//判断是否挂在根上，根上不需要路径
						isRootComponent = behav.gameObject == root;
						if (isRootComponent) break;

						if (behav.gameObject == tran.gameObject)
						{
							path = allPath[tran];
							break;
						}
					}
					else
					{
						if (tran.gameObject == obj)
						{
							path = allPath[tran];
							break;
						}
					}
				}

				if (obj is GameObject)
				{
					codeAssignText.AppendFormat(assignGameObjectCodeFmt, varNameToShow, path);
				}
				else
				{
					if (isRootComponent)
					{
						codeAssignText.AppendFormat(assignRootCodeFmt, varNameToShow, obj.GetType().Name);
					}
					else
					{
						codeAssignText.AppendFormat(assignCodeFmt, varNameToShow, path, obj.GetType().Name);
					}
				}
			}

			codeAssignText.Append(methodEnd);
			codeAssignText.Append(regionEnd);
			//Debug.Log(codeAssignText.ToString());
		}

		private Dictionary<Transform, string> GetChildrenPaths(GameObject rootGo)
		{
			Dictionary<Transform, string> pathDic = new Dictionary<Transform, string>();
			string path = string.Empty;
			Transform[] tfArray = rootGo.GetComponentsInChildren<Transform>(true);
			for (int i = 0; i < tfArray.Length; i++)
			{
				Transform node = tfArray[i];

				string nodeName = node.name;
				while (node.parent != null && node.gameObject != rootGo && node.parent.gameObject != rootGo)
				{
					nodeName = string.Format("{0}/{1}", node.parent.name, nodeName);
					node = node.parent;
				}
				path += string.Format("{0}\n", nodeName);

				if (!pathDic.ContainsKey(tfArray[i]))
				{
					pathDic.Add(tfArray[i], nodeName);
				}
			}
			//Debug.Log(path);
			return pathDic;
		}

		private void CreateCsUIScript()
		{
			string path = EditorPrefs.GetString("create_script_folder", "");
			path = EditorUtility.SaveFilePanel("Save Script", path, root.name + ".cs", "cs");
			if (string.IsNullOrEmpty(path)) return;

			int index = path.LastIndexOf('/');
			className = path.Substring(index + 1, path.LastIndexOf('.') - index - 1);

			StringBuilder scriptContent = new StringBuilder();
			scriptContent.Append(UICodeBuilderConfig.codeAnnotation);
			scriptContent.Append(GetNamespaceString());

			if (selectedPopUpSuperclassIndex == (int) UICodeBuilderConfig.SuperclassIndex.BaseView)
			{
				scriptContent.AppendFormat(UICodeBuilderConfig.baseviewContextClass, className);
			}

			string interfacesStr = GetInterfacesString();
			string clazzStr = GetClazzString(className, interfacesStr);
			scriptContent.Append(clazzStr);

			scriptContent.Append(codeStateText);
			scriptContent.Append(codeStructureText);
			scriptContent.Append(codeAssignText);
			scriptContent.Append(codeEventText);

			scriptContent.Append(UICodeBuilderConfig.classEnd);

			try
			{
				File.WriteAllText(path, scriptContent.ToString(), new UTF8Encoding(false));
				AssetDatabase.Refresh();

				Debug.Log("<color=#F8ED72>Good job! A script is created at: " + path + "</color>");
				EditorPrefs.SetString("create_script_folder", path);
			}
			catch (System.Exception e)
			{
				Debug.LogError(e.Message);
			}
		}

		private string GetNamespaceString()
		{
			string namespaceStr = UICodeBuilderConfig.usingNamespace;
			if (selectedPopUpSuperclassIndex == (int) UICodeBuilderConfig.SuperclassIndex.BaseView)
			{
				namespaceStr += UICodeBuilderConfig.usingBaseViewNamespace;
			}
			return namespaceStr;
		}

		private string GetInterfacesString()
		{
			string interfaces = "";
			foreach (var kv in toogleDicInterfaces)
			{
				if (kv.Value)
				{
					interfaces += (", " + kv.Key);
				}
			}

			if (selectedPopUpSuperclassIndex == (int) UICodeBuilderConfig.SuperclassIndex.NONE &&
				!interfaces.Equals(""))
			{
				int invalidSymbol = interfaces.IndexOf(',');
				interfaces = interfaces.Substring(invalidSymbol + 1);
				interfaces = " :" + interfaces;
			}

			return interfaces;
		}

		private string GetClazzString(string className, string interfaces)
		{
			string clazzStr = "";
			string clazzFormat = "{0}";
			if (selectedPopUpSuperclassIndex == (int) UICodeBuilderConfig.SuperclassIndex.MonoBehaviour)
			{
				clazzFormat = UICodeBuilderConfig.classMonoStart;
			}
			else if (selectedPopUpSuperclassIndex == (int) UICodeBuilderConfig.SuperclassIndex.BaseView)
			{
				clazzFormat = UICodeBuilderConfig.classBaseViewStart;
			}
			else if (selectedPopUpSuperclassIndex == (int) UICodeBuilderConfig.SuperclassIndex.NONE)
			{
				clazzFormat = UICodeBuilderConfig.classStart;
			}
			clazzStr = string.Format(clazzFormat, className, interfaces);
			return clazzStr;
		}

		private void AddScriptComponent()
		{
			if (CheckEditorIsCompiling() || !CheckScriptSaved())
			{
				return;
			}

			Assembly[] AssbyCustmList = System.AppDomain.CurrentDomain.GetAssemblies();
			Assembly asCSharp = null;
			for (int i = 0; i < AssbyCustmList.Length; i++)
			{
				string assbyName = AssbyCustmList[i].GetName().Name;
				if (assbyName == "Assembly-CSharp")
				{
					asCSharp = AssbyCustmList[i];
					break;
				}
			}

			scriptType = asCSharp.GetType(className);
			if (scriptType == null)
			{
				EditorUtility.DisplayDialog("Warning", "Add script component failed! Please check your script is correct.", "OK");
				return;
			}
			else
			{
				Component targetComponent = root.GetComponent(scriptType);
				if (targetComponent == null)
				{
					targetComponent = root.AddComponent(scriptType);
				}
			}
		}

		private void BindSerializeWidgetRefrences()
		{
			if (CheckEditorIsCompiling() || !CheckScriptSaved() || !CheckIsAddedScriptComponent())
			{
				return;
			}

			Component targetComponent = root.GetComponent(scriptType);
			if (targetComponent == null)
			{
				targetComponent = root.AddComponent(scriptType);
			}

			//资源刷新以后variableNameDic被清空了= =再获取一遍吧
			if (variableNameDic.Count == 0)
			{
				BuildStatementCode();
			}

			BindingFlags flags = BindingFlags.SetField | BindingFlags.Instance | (IsPublic() ? BindingFlags.Public : BindingFlags.NonPublic);
			foreach (string vName in variableNameDic.Keys)
			{
				if (!string.IsNullOrEmpty(vName))
				{
					try
					{
						string varNameToShow = vName.Replace("_", "");
						scriptType.InvokeMember(varNameToShow, flags, null, targetComponent, new object[] { variableNameDic[vName] }, null, null, null);
					}
					catch (System.Exception e)
					{
						Debug.LogWarning(e.Message);
					}
				}
			}
		}
	}
}