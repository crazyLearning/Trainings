import {IInputs, IOutputs} from "./generated/ManifestTypes";

export class WordCounterControl implements ComponentFramework.StandardControl<IInputs, IOutputs> {

    /**
     * GLobal variables
     */
    private container:HTMLDivElement;
    private inputText:HTMLTextAreaElement;
    private labelText:HTMLLabelElement;
    private remainingCount:number;
    private context:ComponentFramework.Context<IInputs>;
    private notifyOutputChanged: () => void;

    /**
     * Empty constructor.
     */
    constructor()
    {

    }

    /**
     * Used to initialize the control instance. Controls can kick off remote server calls and other initialization actions here.
     * Data-set values are not initialized here, use updateView.
     * @param context The entire property bag available to control via Context Object; It contains values as set up by the customizer mapped to property names defined in the manifest, as well as utility functions.
     * @param notifyOutputChanged A callback method to alert the framework that the control has new outputs ready to be retrieved asynchronously.
     * @param state A piece of data that persists in one session for a single user. Can be set at any point in a controls life cycle by calling 'setControlState' in the Mode interface.
     * @param container If a control is marked control-type='standard', it will receive an empty div element within which it can render its content.
     */
    public init(context: ComponentFramework.Context<IInputs>, notifyOutputChanged: () => void, state: ComponentFramework.Dictionary, container:HTMLDivElement): void
    {
        // Add control initialization code
        this.context = context;
        this.container = container;
        this.notifyOutputChanged = notifyOutputChanged;
        this.inputText = document.createElement("textarea");
        this.container.appendChild(this.inputText);
        this.inputText.setAttribute("style","width:100%; border:solid 1px #FF0000;box-shadow:0px 4px 5px #FFA500");
        this.inputText.setAttribute("rows","5");

        this.inputText.value = context.parameters.commentField.raw || "";
        this.inputText.addEventListener("keyup",this.onKeyUp.bind(this));
        this.remainingCount =context.parameters.wordLimit.raw || 0;     
        this.labelText= document.createElement("label");
        this.container.appendChild(this.labelText);
        this.labelText.innerHTML = "maximum limit is " + context.parameters.wordLimit.raw + " Remaining : "+this.remainingCount || "";
        
    }


    /**
     * Called when any value in the property bag has changed. This includes field values, data-sets, global values such as container height and width, offline status, control metadata values such as label, visible, etc.
     * @param context The entire property bag available to control via Context Object; It contains values as set up by the customizer mapped to names defined in the manifest, as well as utility functions
     */
    public updateView(context: ComponentFramework.Context<IInputs>): void
    {
        // Add code to update control view
        this.remainingCount = (context.parameters.wordLimit.raw || 0) - this.inputText.value.length;  
        this.labelText.innerHTML = "maximum limit is " + context.parameters.wordLimit.raw + " Remaining : "+this.remainingCount || "";

    }

    /**
     * It is called by the framework prior to a control receiving new data.
     * @returns an object based on nomenclature defined in manifest, expecting object[s] for property marked as “bound” or “output”
     */
    public getOutputs(): IOutputs
    {
        return {
            commentField:this.inputText.value
        };
    }

    /**
     * Called when the control is to be removed from the DOM tree. Controls should use this call for cleanup.
     * i.e. cancelling any pending remote calls, removing listeners, etc.
     */
    public destroy(): void
    {
        // Add code to cleanup control if necessary
    }

    public onKeyUp(): void
    {
        // Add code to cleanup control if necessary
        this.remainingCount = (this.context.parameters.wordLimit.raw || 0) - this.inputText.value.length;  
        this.labelText.innerHTML = "maximum limit is " + this.context.parameters.wordLimit.raw + " Remaining : "+this.remainingCount || "";
        if(this.remainingCount==0)
        {
            this.inputText.setAttribute("maxlength","500");
        }
        this.notifyOutputChanged();
    }
}
