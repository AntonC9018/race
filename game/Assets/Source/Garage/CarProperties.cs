using System;
using System.IO;
using System.Xml.Serialization;
using EngineCommon.ColorPicker;
using UnityEngine;

namespace Garage
{
    /// <summary>
    /// Stores the actual source of truth data associated with the car.
    /// Other systems should synchronize to this data.
    /// Provides no means of actually setting the values, for that use `CarProperties`.
    /// </summary>
    [System.Serializable]
    public class CarDataModel
    {
        /// <summary>
        /// </summary>
        public Color mainColor;

        /// <summary>
        /// </summary>
        public string name;

        // I'm not using notifyPropertyChanged or whatnot, because I want to have more control
        // over these things. I love declarative programming, but I want to do it with my
        // code generator when possible to have max control over things.
    }

    /// <summary>
    /// Provides data binding between the currently selected car and the other systems
    /// that need to get notified when that data changes.
    /// </summary>
    public class CarProperties : MonoBehaviour
    {
        // TODO: codegen stuff.

        /// <summary>
        /// </summary>
        internal CarDataModel _currentModel;

        // For now, to avoid creating tiny scripts that do almost nothing,
        // just reference the color picker and the car mesh renderer here,
        // but when the property count grows, events can be used to decouple things.
        // I'm doing the simple thing here for now, it's not necessarily scalable
        // in the long run.
        // TODO: Could also consider the mesh renderer as the source of truth,
        // it's not clear what I want yet anyway.
        [SerializeField] internal MeshRenderer _carMeshRenderer;

        // For example, here, we could use a bridge script that would listen to the picker events,
        // set the color here, which would notify it back, which it should ignore.
        // And the other way, when the data changes here, it would update it on the picker, and ignore
        // the event coming from that.
        [SerializeField] internal CUIColorPicker _colorPicker;

        void OnEnable()
        {
            Debug.Assert(_carMeshRenderer != null);
            ResetModelWithCarDataPotentiallyFromFile(_carMeshRenderer);

            _colorPicker.OnValueChangedEvent.AddListener(OnPickerColorSet);
        }
        
        void OnDisable()
        {
            if (_colorPicker != null)
                _colorPicker.OnValueChangedEvent.RemoveListener(OnPickerColorSet);
            
            WriteModel(_currentModel, GetFilePath(_currentModel.name));
        }


        // TODO: might want to customize the way we get the file path.
        public static string GetFilePath(string carName)
        {
            return Application.persistentDataPath + "/cardata_" + carName + ".xml";
        }

        public void SelectCurrentCar(MeshRenderer otherCarsMeshRenderer)
        {
            // Do this check for public methods.
            if (otherCarsMeshRenderer == _carMeshRenderer)
                return;

            _carMeshRenderer = otherCarsMeshRenderer;

            // Let's say the object's name is how we store the data.
            // Let's say we store it in XML for now.
            // TODO: might be worth it to pack this one.

            // if (!File.Exists(previousCarFullFilePath))
            {
                var previousCarFullFilePath = GetFilePath(_currentModel.name);
                // TODO: keep a dirty flag and only overwrite the file if anything ever changed.
                WriteModel(_currentModel, previousCarFullFilePath);
            }

            ResetModelWithCarDataPotentiallyFromFile(otherCarsMeshRenderer);
        }

        internal void ResetModelWithCarDataPotentiallyFromFile(MeshRenderer meshRenderer)
        {
            var dataFullFilePath = GetFilePath(meshRenderer.name);

            if (File.Exists(dataFullFilePath))
            {
                var serializer = new XmlSerializer(typeof(CarDataModel));
                using var textReader = new StreamReader(dataFullFilePath);
                var model = (CarDataModel) serializer.Deserialize(textReader);
                ResetModel((CarDataModel) model, meshRenderer);
            }
            else
            {
                var model = new CarDataModel();
                model.mainColor = meshRenderer.material.color;
                model.name = meshRenderer.name;

                ResetModel(model, meshRenderer);
            }
        }

        private static void WriteModel(CarDataModel model, string fileName)
        {
            var serializer = new XmlSerializer(typeof(CarDataModel));
            using var textWriter = new StreamWriter(fileName);
            serializer.Serialize(textWriter, model);
        }

        public struct PropertySetContext
        {
            public CarDataModel model;
            
            // TODO: Should be a codegened enum probably 
            public string nameOfPropertyThatChanged;

            // other data...
            // public GameObject newCarGameObject;
        }

        // TODO: This is messy, because the MeshRenderer wants really hard to be the source of truth here.
        internal void ResetModel(CarDataModel model, MeshRenderer carMeshRenderer)
        {
            _currentModel = model;

            // TODO: Fire callbacks and whatnot.
            carMeshRenderer.material.color = model.mainColor;
            
            // Debug.Log("Setting color " + model.mainColor.ToString());
            _colorPicker.ColorRGB = model.mainColor;
        }

        /// <summary>
        /// Callback used for the color picker.
        /// </summary>
        public void OnPickerColorSet(Color color)
        {
            if (_currentModel.mainColor != color)
            {
                _currentModel.mainColor = color;

                _carMeshRenderer.material.color = _currentModel.mainColor;

                // Currently, the only source that the data comes from is the color picker,
                // so i'm just not resetting it here.
                // But with an event system, that would be the responsibility of a bridge script.

                // TODO: fire callbacks.
                // PropertySetContext context;
                // context.model = _currentModel;
                // context.nameOfPropertyThatChanged = nameof(CarDataModel.mainColor);
            }
        }
    }
}