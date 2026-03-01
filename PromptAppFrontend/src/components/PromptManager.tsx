import { useState, useEffect } from "react";
import axios from "axios";
import type { Prompt, PromptRequest } from "../types";

export default function PromptManager() {
  const [prompts, setPrompts] = useState<Prompt[]>([]);
  const [newPrompt, setNewPrompt] = useState<string>("");

  const addPrompt = async () => {
    if (!newPrompt.trim()) return;
    try {
      const request: PromptRequest = { content: newPrompt };
      const response = await axios.post("http://localhost:5001/api/prompts", request);
      const newP: Prompt = {
        id: response.data.id,
        content: newPrompt,
        state: "Queued",
        result: null
      };
      setPrompts(prev => [...prev, newP]);
      setNewPrompt("");
    } catch (error) {
      console.error("Error adding prompt:", error);
    }
  };

useEffect(() => {
  const interval = setInterval(async () => {
    setPrompts(prev => {
      Promise.all(prev.map(async (p) => {
        const res = await axios.get(`http://localhost:5001/api/prompts/${p.id}`);
        return { ...p, state: res.data.state, result: res.data.result };
      })).then(setPrompts);
      return prev;
    });
  }, 2000);

  return () => clearInterval(interval);
}, []);

  return (
    <div>
      <h1>Prompt Manager</h1>
        <input
          type="text"
          value={newPrompt}
          onChange={(e) => setNewPrompt(e.target.value)}
          placeholder="Type your prompt..."
        />
        <button onClick={addPrompt}>Add Prompt</button>

      <h2>Prompt List</h2>
      <ul>
        {prompts.map(p => (
          <li key={p.id}>
            <strong>{p.content}</strong> - Status: {p.state} {p.result && `- Result: ${p.result}`}
          </li>
        ))}
      </ul>
    </div>
  )
}