using UnityEngine;
#if INPUT_SYSTEM_ENABLED
using Input = XCharts.Runtime.InputHelper;
#endif
using XCharts.Runtime;

public class BarChartExample : MonoBehaviour
{
    public BarChart barchart;
    void Start()
    {
        //initialize();
        initialize1();
    }

    void Update()
    {
        // Uncomment the following line if you want to add new data when space is pressed
        // if (Input.GetKeyDown(KeyCode.Space)) AddData();
    }

    
    void initialize1()
    {
        // Get or add the LineChart component
        barchart = gameObject.GetComponent<BarChart>();
        if (barchart == null)
        {
            barchart = gameObject.AddComponent<BarChart>();
            barchart.Init();
        }

        // Set chart title and tooltip visibility
        barchart.EnsureChartComponent<Title>().show = true;
        barchart.EnsureChartComponent<Title>().text = "Weekly Data Bar Chart";

        barchart.EnsureChartComponent<Tooltip>().show = true;
        barchart.EnsureChartComponent<Legend>().show = true;

        // Ensure x and y axes are created
        var xAxis = barchart.EnsureChartComponent<XAxis>();
        var yAxis = barchart.EnsureChartComponent<YAxis>();
        xAxis.show = true;
        yAxis.show = true;
        xAxis.type = Axis.AxisType.Category; // Set x-axis type to Category
        yAxis.type = Axis.AxisType.Value; // Set y-axis type to Value
        yAxis.min = -2; // Set this to 0 to make sure bars start from the y=0 line
        yAxis.max = 30; // You can adjust the maximum value as needed
        // Define weekdays for x-axis
        string[] weekdays = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

        // Configure the x-axis with the weekdays
        xAxis.data = new System.Collections.Generic.List<string>(weekdays);
        xAxis.splitNumber = 7; // Match number of weekdays

        // Clear existing data and create a new line series
        barchart.RemoveData();
        barchart.AddSerie<Bar>();

        // Add random data for each weekday
        for (int i = 0; i < weekdays.Length; i++)
        {
            barchart.AddXAxisData(weekdays[i]); // Add x-axis data
            barchart.AddData(0, Random.Range(10f, 20f)); // Random float values for the y-axis
        }
    }
}
