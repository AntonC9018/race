bl_info = {
    "name" : "FBX export",
    "author" : "Anton Curmanschii",
    "description" : "",
    "blender" : (2, 80, 0),
    "version" : (0, 0, 1),
    "location" : "",
    "warning" : "",
    "category" : "Generic"
}

import bpy

class ConvertCollectionToEmptyOperator(bpy.types.Operator):
    """Tooltip"""

    bl_idname = "object.convert_collection_to_empty"
    bl_label = "Convert Collection to Empty"

    @classmethod
    def poll(cls, context):
        return context.collection is not None

    def execute(self, context):
        print(context.collection.name)
        return {'FINISHED'}

    @staticmethod
    def menu_func(self, context):
        self.layout.operator(ConvertCollectionToEmptyOperator.bl_idname, text=ConvertCollectionToEmptyOperator.bl_label)

    # Register and add to the "object" menu (required to also use F3 search "Simple Object Operator" for quick access)
    @staticmethod
    def register():
        bpy.utils.register_class(ConvertCollectionToEmptyOperator)
        bpy.types.VIEW3D_MT_object.append(ConvertCollectionToEmptyOperator.menu_func)

    @staticmethod
    def unregister():
        bpy.utils.unregister_class(ConvertCollectionToEmptyOperator)
        bpy.types.VIEW3D_MT_object.remove(ConvertCollectionToEmptyOperator.menu_func)


def register():
    print("Start")
    ConvertCollectionToEmptyOperator.register()

def unregister():
    print("End")
    ConvertCollectionToEmptyOperator.unregister()
